// <copyright file="ModerationModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Services;

    /// <summary>
    /// Module for Moderative uses.
    /// </summary>
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Dictionary<string, int> TimeUnits = new Dictionary<string, int>
        {
            { "second",    1000 },
            { "seconds",   1000 },
            { "minute",    1000 * 60 },
            { "minutes",   1000 * 60 },
            { "hour",      1000 * 60 * 60 },
            { "hours",     1000 * 60 * 60 },
            { "day",       1000 * 60 * 60 * 24 },
            { "days",      1000 * 60 * 60 * 24 },
        };

        /// <summary>
        /// Command to ban a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <returns></returns>
        [Command("ban")]
        [Alias("gulag", "getout")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IUser user)
        {
            if (Context.Guild == null) return;
            var delTask = Context.Message.DeleteAsync();

            await Context.Guild.AddBanAsync(user);
            await delTask;
            await ReplyAsync($"User {user.Username} has been banned by {Context.User.Mention}.");
        }

        /// <summary>
        /// Command to kick a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <returns></returns>
        [Command("kick")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user)
        {
            var delTask = Context.Message.DeleteAsync();

            var kickTask = user.KickAsync();
            await delTask;
            if (kickTask == null) return;
            await kickTask;
            await ReplyAsync($"User {user.Username} has been kicked by {Context.User.Mention}.");
        }

        /// <summary>
        /// Command to mute a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <param name="amount">A timespan in full timeunits.</param>
        /// <param name="unitName">The name of the unit.</param>
        /// <returns></returns>
        [Command("mute")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Mute(IGuildUser user, int amount = 0, string unitName = "")
        {
            var delTask = Context.Message.DeleteAsync();

            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));
            var duration = 0;
            try
            {
                duration = amount * TimeUnits[unitName];
            }
            catch (KeyNotFoundException)
            {
            }

            if (duration == 0)
            {
                await user.AddRoleAsync(mutedRole);
                await delTask;
                await ReplyAsync($"User {user.Mention} has been muted by {Context.User.Mention}.");
                return;
            }
            else if (duration < 0)
            {
                await ReplyAsync($"User <@{user.Id}> has not been muted, since the duration of the mute was negative.");
                return;
            }

            await user.AddRoleAsync(mutedRole);
            await delTask;
            await ReplyAsync($"User {user.Mention} has been muted by {Context.User.Mention} for {amount} {unitName}.");
            await Task.Delay(duration);

            await user.RemoveRoleAsync(mutedRole);
            await ReplyAsync($"User <@{user.Id}> has been unmuted automatically.");
        }

        /// <summary>
        /// Command to unmute a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <returns></returns>
        [Command("unmute")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Unmute(IGuildUser user)
        {
            var delTask = Context.Message.DeleteAsync();
            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.RemoveRoleAsync(mutedRole);
            await delTask;
            await ReplyAsync($"User {user.Mention} has been unmuted by {Context.User.Mention}.");
        }

        /// <summary>
        /// Command group to manage temporary restrictions to channels.
        /// </summary>
        [Group("stop")]
        public class StopModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Command to restrict sending messages to the channel.
            /// </summary>
            /// <returns></returns>
            [Command("on")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task On()
            {
                var channel = Context.Channel as IGuildChannel;

                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been restricted.").WithColor(new Color(244, 67, 54));
                await ReplyAsync(string.Empty, embed: embed.Build());
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(sendMessages: PermValue.Allow));
            }

            /// <summary>
            /// Command to revoke an earlier restriction to the channel.
            /// </summary>
            /// <returns></returns>
            [Command("off")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Off()
            {
                var channel = Context.Channel as IGuildChannel;

                await channel.AddPermissionOverwriteAsync(Context.User, default(OverwritePermissions));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, default(OverwritePermissions));
                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been unrestricted.").WithColor(new Color(139, 195, 74));
                await ReplyAsync(string.Empty, embed: embed.Build());
            }
        }

        /// <summary>
        /// Command group to manage image spamming.
        /// </summary>
        [Group("imagespam")]
        public class ImageSpamModule : ModuleBase<SocketCommandContext>
        {
            private readonly ModerationService service;

            /// <summary>
            /// Initializes a new instance of the <see cref="ImageSpamModule"/> class.
            /// </summary>
            /// <param name="svc">Backing service instance.</param>
            public ImageSpamModule(ModerationService svc)
            {
                service = svc;
            }

            /// <summary>
            /// Command to block image spam in this channel.
            /// </summary>
            /// <returns></returns>
            [Command("block")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Block()
            {
                var channel = Context.Channel as ITextChannel;
                if (service.AddBinding(channel))
                    await ReplyAsync($"Image spam in this channel (#{channel.Name}) is now blocked.");
                else
                    await ReplyAsync("Image spam in this channel is already blocked.");
            }

            /// <summary>
            /// Command to revoke a prior image spam block.
            /// </summary>
            /// <returns></returns>
            [Command("unblock")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Unblock()
            {
                var channel = Context.Channel as ITextChannel;
                if (service.RemoveBinding(channel))
                    await ReplyAsync($"Image spam in this channel (#{channel.Name}) is no longer blocked.");
                else
                    await ReplyAsync("Image spam in this channel was not blocked.");
            }

            /// <summary>
            /// Command to check if image spam is being blocked.
            /// </summary>
            /// <returns></returns>
            [Command("check")]
            [RequireContext(ContextType.Guild)]
            public async Task Check()
            {
                var channel = Context.Channel as ITextChannel;
                if (service.CheckBinding(channel))
                    await ReplyAsync("Image spam in this channel is blocked. Sending more than three images within 15 seconds will get you muted.");
                else
                    await ReplyAsync("Image spam in this channel is not restricted.");
            }
        }

        /// <summary>
        /// Command group to manage command usage for specific users.
        /// </summary>
        [Group("blacklist")]
        public class BlacklistModule : ModuleBase<SocketCommandContext>
        {
            private readonly BlacklistService service;

            public BlacklistModule(BlacklistService svc)
            {
                service = svc;
            }

            /// <summary>
            /// Command to add a specific user to the bot's blacklist.
            /// </summary>
            /// <param name="user">The user in question.</param>
            /// <returns></returns>
            [Command("add")]
            public async Task Add(IGuildUser user)
            {
                if (service.AddBinding(Context.Guild, user))
                    await ReplyAsync($"Command usage on this server is now blocked for #{user.Mention}.");
                else
                    await ReplyAsync("Command usage on this server is already blocked for that user.");
            }

            /// <summary>
            /// Command to remove a specific user from the bot's blacklist.
            /// </summary>
            /// <param name="user">The user in question.</param>
            /// <returns></returns>
            [Command("remove")]
            public async Task Remove(IGuildUser user)
            {
                if (service.RemoveBinding(Context.Guild, user))
                    await ReplyAsync($"Command usage on this server is now no longer blocked for #{user.Mention}.");
                else
                    await ReplyAsync("Command usage on this server was not blocked for that user.");
            }

            /// <summary>
            /// Command to check if a specific user is on the bot's blacklist.
            /// </summary>
            /// <param name="user">The user in question.</param>
            /// <returns></returns>
            [Command("check")]
            public async Task Check(IGuildUser user)
            {
                if (service.CheckBinding(Context.Guild, user))
                    await ReplyAsync($"Command usage on this server is currently blocked for #{user.Mention}.");
                else
                    await ReplyAsync("Command usage on this server is currently not blocked for that user.");
            }
        }
    }
}
