// <copyright file="ModerationService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services.TimedServices
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Databases;

    using Discord;
    using Discord.WebSocket;

    using Extensions;

    using Microsoft.EntityFrameworkCore;

    using static Databases.ModerationContext;

    public class ModerationService : ITimedService, IDatabaseService
    {
        private DbContextOptions dbOptions;

        /// <inheritdoc />
        public void Init(dynamic config, IServiceProvider map)
        {
            dbOptions = map.GetService(typeof(DbContextOptions)) as DbContextOptions ?? throw new ServiceNotFoundException("Database Options were not found in service provider.");

            var client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Discord Client was not found in service provider.");

            client.UserJoined += UserJoinedHandler;
            client.UserLeft += UserLeftHandler;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                var guildBinding = await db.GuildBindings.FindAsync(binding.Id.ToString());

                if (guildBinding == null) return false;

                await guildBinding.Role.DeleteAsync();
                return true;
            }
        }

        /// <inheritdoc />
        public async Task Callback()
        {
            using (var db = new ModerationContext(dbOptions))
            {
                var expiredMutes = db.GuildUserMuteBindings.Include(m => m.GuildBinding)
                    .Where(b => b.MutedUntil <= DateTime.UtcNow);

                await expiredMutes.ForEachAsync(async b =>
                    {
                        await b.User.RemoveRoleAsync(b.GuildBinding.Role);
                    });

                db.GuildUserMuteBindings.RemoveRange(expiredMutes);
                await db.SaveChangesAsync();
            }
        }

        public async Task<BindingStatus> AddBinding(IRole role)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                try
                {
                    if (await db.GuildBindings.FindAsync(role.Guild.Id.ToString()) != null)
                        return BindingStatus.PreconditionFailed;

                    db.GuildBindings.Add(new GuildBinding
                    {
                        Guild = role.Guild,
                        Role = role,
                    });
                    await db.SaveChangesAsync();
                    return BindingStatus.Success;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        public async Task<BindingStatus> UpdateBinding(IRole role)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                try
                {
                    var binding = await db.GuildBindings.FindAsync(role.Guild.Id.ToString());
                    if (binding == null) return BindingStatus.PreconditionFailed;

                    binding.Role = role;
                    db.GuildBindings.Update(binding);
                    await db.SaveChangesAsync();
                    return BindingStatus.Success;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        public async Task<IRole> GetRole(IGuild guild)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                return (await db.GuildBindings.FindAsync(guild.Id.ToString()))?.Role;
            }
        }

        public async Task<BindingStatus> RemoveRole(IGuild guild)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                try
                {
                    var guildBinding = await db.GuildBindings.FindAsync(guild.Id.ToString());
                    if (guildBinding == null)
                        return BindingStatus.PreconditionFailed;

                    db.GuildBindings.Remove(guildBinding);
                    await db.SaveChangesAsync();
                    return BindingStatus.Success;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        public async Task<BindingStatus> AddMute(IGuildUser user, DateTime? until = null)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                try
                {
                    if (await db.GuildUserMuteBindings.FindAsync(user.Guild.Id.ToString(), user.Id.ToString()) != null)
                        return BindingStatus.PreconditionFailed;

                    var mute = db.GuildUserMuteBindings.Add(new GuildUserMuteBinding
                    {
                        Guild = user.Guild,
                        MutedUntil = until,
                        User = user,
                    });
                    await db.SaveChangesAsync();

                    await mute.Reference(m => m.GuildBinding).LoadAsync();

                    await user.AddRoleAsync(mute.Entity.GuildBinding.Role);
                    return BindingStatus.Success;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        public async Task<BindingStatus> RemoveMute(IGuildUser user)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                try
                {
                    var binding = await db.GuildUserMuteBindings.FindAsync(user.Guild.Id.ToString(), user.Id.ToString());
                    if (binding == null) return BindingStatus.PreconditionFailed;

                    await db.Entry(binding).Reference(m => m.GuildBinding).LoadAsync();
                    var role = binding.GuildBinding.Role;

                    db.GuildUserMuteBindings.Remove(binding);
                    await db.SaveChangesAsync();

                    await user.RemoveRoleAsync(role);
                    return BindingStatus.Success;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        private async Task UserJoinedHandler(SocketGuildUser user)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                var binding = await db.GuildUserMuteBindings.FindAsync(user.Guild.Id.ToString(), user.Id.ToString());
                if (binding == null) return;

                await db.Entry(binding).Reference(m => m.GuildBinding).LoadAsync();
                await binding.User.AddRoleAsync(binding.GuildBinding.Role);
            }
        }

        private async Task UserLeftHandler(SocketGuildUser user)
        {
            using (var db = new ModerationContext(dbOptions))
            {
                var binding = await db.GuildUserMuteBindings.FindAsync(user.Guild.Id.ToString(), user.Id.ToString());
                if (binding == null || (binding.MutedUntil.HasValue &&
                                        binding.MutedUntil - DateTime.UtcNow < TimeSpan.FromDays(7))) return;

                binding.MutedUntil = DateTime.UtcNow + TimeSpan.FromDays(7);
                db.GuildUserMuteBindings.Update(binding);
                await db.SaveChangesAsync();
            }
        }

        public class AutoUnmuteService
        {
            public IGuildUser User { get; set; }

            public IRole Role { get; set; }

            public ITextChannel Channel { get; set; }

            public TimeSpan TimeSpan { get; set; }

            public async Task AutoUnmute()
            {
                await Task.Delay(TimeSpan);
                await User.RemoveRoleAsync(Role);
                await Channel.SendEmbedAsync($"User {User.Mention} has been unmuted automatically.");
            }
        }
    }
}
