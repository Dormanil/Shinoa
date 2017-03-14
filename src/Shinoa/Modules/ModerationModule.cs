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

    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Dictionary<string, int> TimeUnits = new Dictionary<string, int>
        {
            { "seconds",    1000 },
            { "minutes",    1000 * 60 },
            { "hours",      1000 * 60 * 60 },
        };

        [Command("ban")]
        [Alias("gulag", "getout")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IUser user)
        {
            if (Context.Guild == null) return;
            var delTask = Context.Message.DeleteAsync();

            await Context.Guild.AddBanAsync(user);
            await delTask;
            await ReplyAsync($"User {user.Username} has been banned by {Context.User.Mention}.");
        }

        [Command("kick")]
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

        [Command("mute")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Mute(IGuildUser user, int amount = 0, string unitName = "")
        {
            var delTask = Context.Message.DeleteAsync();

            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.AddRolesAsync(mutedRole);
            await delTask;

            if (amount == 0)
            {
                await ReplyAsync($"User {user.Mention} has been muted by {Context.User.Mention}.");
            }
            else
            {
                var duration = amount * TimeUnits[unitName];
                await ReplyAsync($"User {user.Mention} has been muted by {Context.User.Mention} for {amount} {unitName}.");
                await Task.Delay(duration);
                await user.RemoveRolesAsync(mutedRole);
                await ReplyAsync($"User <@{user.Id}> has been unmuted automatically.");
            }
        }

        [Command("unmute")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Unmute(IGuildUser user)
        {
            var delTask = Context.Message.DeleteAsync();
            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.RemoveRolesAsync(mutedRole);
            await delTask;
            await ReplyAsync($"User {user.Mention} has been unmuted by {Context.User.Mention}.");
        }

        [Group("stop")]
        public class StopModule : ModuleBase<SocketCommandContext>
        {
            [Command("on")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task On()
            {
                var channel = Context.Channel as IGuildChannel;

                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been restricted.").WithColor(new Color(244, 67, 54));
                await ReplyAsync(string.Empty, embed: embed.Build());
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(sendMessages: PermValue.Allow));
            }

            [Command("off")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Off()
            {
                var channel = Context.Channel as IGuildChannel;

                await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(sendMessages: PermValue.Inherit));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Inherit));
                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been unrestricted.").WithColor(new Color(139, 195, 74));
                await ReplyAsync(string.Empty, embed: embed.Build());
            }
        }

        [Group("imagespam")]
        public class ImageSpamModule : ModuleBase<SocketCommandContext>
        {
            private ModerationService service;

            public ImageSpamModule(ModerationService svc)
            {
                service = svc;
            }

            [Command("block")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Block()
            {
                var channel = Context.Channel as ITextChannel;
                if (service.AddBinding(channel))
                {
                    await ReplyAsync($"Image spam in this channel (#{channel.Name}) is now blocked.");
                }
                else
                {
                    await ReplyAsync("Image spam in this channel is already blocked.");
                }
            }

            [Command("unblock")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Unblock()
            {
                var channel = Context.Channel as ITextChannel;
                if (service.RemoveBinding(channel))
                {
                    await ReplyAsync($"Image spam in this channel (#{channel.Name}) is no longer blocked.");
                }
                else
                {
                    await ReplyAsync("Image spam in this channel was not blocked.");
                }
            }

            [Command("check")]
            public async Task Check()
            {
                var channel = Context.Channel as ITextChannel;
                if (service.CheckBinding(channel))
                {
                    await ReplyAsync(
                        "Image spam in this channel is blocked. Send more than three images within 15 seconds will get you muted.");
                }
                else
                {
                    await ReplyAsync("Image spam in this channel is not restricted.");
                }
            }
        }
    }
}
