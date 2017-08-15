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
    using Microsoft.Extensions.DependencyInjection;

    using static Databases.ModerationContext;

    /// <summary>
    /// Service for moderative tasks.
    /// </summary>
    public class ModerationService : ITimedService, IDatabaseService
    {
        /// <inheritdoc />
        public void Init(dynamic config, IServiceProvider map)
        {
            var client = map.GetService<DiscordSocketClient>() ?? throw new ServiceNotFoundException("Discord Client was not found in service provider.");

            client.UserJoined += UserJoinedHandler;
            client.UserLeft += UserLeftHandler;
        }

        /// <inheritdoc />
        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                var bindings = db.GuildUserMuteBindings.Where(b => b.GuildId == binding.Id);
                var roleBindings = db.GuildRoleBindings.Where(b => b.GuildId == binding.Id);

                if (!(bindings.Any() || roleBindings.Any()))
                    return false;

                try
                {
                    await roleBindings.Single().Role.DeleteAsync();
                }
                catch (InvalidOperationException)
                {
                    return false;
                }

                return true;
            }
        }

        /// <inheritdoc />
        public async Task Callback()
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                var expiredMutes = db.GuildUserMuteBindings.Where(b => b.MuteTime <= DateTime.Now);
                var roleBindings = db.GuildRoleBindings.Where(b => expiredMutes.Any(m => m.GuildId == b.GuildId));

                await expiredMutes.ForEachAsync(async b =>
                    {
                        var role = (await roleBindings.SingleOrDefaultAsync(binding => b.GuildId == binding.GuildId)).Role;
                        await b.User.RemoveRoleAsync(role);
                    });

                db.GuildUserMuteBindings.RemoveRange(expiredMutes);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Adds an <see cref="IRole"/> for a specific <see cref="IGuild"/> to the database.
        /// </summary>
        /// <param name="guild"><see cref="IGuild"/> to add to the database.</param>
        /// <param name="mutedRole"><see cref="IRole"/> to add to the database.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation.</returns>
        public async Task<BindingStatus> AddRole(IGuild guild, IRole mutedRole)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                try
                {
                    if (await db.GuildRoleBindings.AnyAsync(b => b.GuildId == guild.Id && b.RoleId == mutedRole.Id))
                        return BindingStatus.AlreadyExists;
                    db.GuildRoleBindings.Add(new GuildRoleBinding
                    {
                        Guild = guild,
                        Role = mutedRole,
                    });

                    await db.SaveChangesAsync();
                    return BindingStatus.Added;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        public IRole GetRole(IGuild guild)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                return db.GuildRoleBindings.Single(b => b.GuildId == guild.Id).Role;
            }
        }

        public async Task<BindingStatus> RemoveRole(IGuild guild, IRole role)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                try
                {
                    if (await db.GuildRoleBindings.AnyAsync(b => b.GuildId == guild.Id && b.RoleId == role.Id))
                        return BindingStatus.NotExisting;
                    db.GuildRoleBindings.Remove(new GuildRoleBinding
                    {
                        Guild = guild,
                        Role = role,
                    });

                    await db.SaveChangesAsync();
                    return BindingStatus.Removed;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        public async Task<BindingStatus> AddMute(IGuild guild, IGuildUser user, DateTime? until = null)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                try
                {
                    if (await db.GuildUserMuteBindings.AnyAsync(b => b.GuildId == guild.Id && b.UserId == user.Id))
                        return BindingStatus.AlreadyExists;
                    db.GuildUserMuteBindings.Add(new GuildUserMuteBinding
                    {
                        Guild = guild,
                        MuteTime = until,
                        User = user,
                    });
                    await db.SaveChangesAsync();
                    return BindingStatus.Added;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        public async Task<BindingStatus> RemoveMute(IGuild guild, IGuildUser user)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                try
                {
                    var binding = db.GuildUserMuteBindings.Where(b => b.GuildId == guild.Id && b.UserId == user.Id);
                    if (await binding.AnyAsync(b => b.GuildId == guild.Id && b.UserId == user.Id))
                        return BindingStatus.NotExisting;

                    db.GuildUserMuteBindings.Remove(binding.Single());
                    await db.SaveChangesAsync();
                    return BindingStatus.Added;
                }
                catch (Exception)
                {
                    return BindingStatus.Error;
                }
            }
        }

        private async Task UserJoinedHandler(SocketGuildUser user)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                var bindings = db.GuildUserMuteBindings.Where(b => b.UserId == user.Id);

                if (!await bindings.AnyAsync()) return;

                var guilds = db.GuildRoleBindings.Where(g => bindings.Any(b => b.GuildId == g.GuildId));
                await bindings.ForEachAsync(async b => await (b.User?.AddRoleAsync(guilds.Single(g => b.GuildId == g.GuildId).Role) ?? Task.CompletedTask));
            }
        }

        private async Task UserLeftHandler(SocketGuildUser user)
        {
            using (var db = Shinoa.Provider.GetService<ModerationContext>())
            {
                var bindings = db.GuildUserMuteBindings.Where(b => b.UserId == user.Id && (b.MuteTime == null || b.MuteTime - DateTime.Now > TimeSpan.FromDays(7)));

                if (!await bindings.AnyAsync()) return;

                bindings = bindings.AsTracking();

                await bindings.ForEachAsync(b => b.MuteTime = DateTime.Now + TimeSpan.FromDays(7));
                await db.SaveChangesAsync();
            }
        }

        public class AutoUnmuteService
        {
            public IGuildUser User { get; set; }

            public IRole Role { get; set; }

            public ITextChannel Channel { get; set; }

            public TimeSpan TimeSpan { get; set; }

            public async void AutoUnmute()
            {
                Thread.Sleep(TimeSpan);
                await User.RemoveRoleAsync(Role);
                await Channel.SendEmbedAsync($"User <@{User.Id}> has been unmuted automatically.");
            }
        }
    }
}
