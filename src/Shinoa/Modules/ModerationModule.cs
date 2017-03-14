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
        private static readonly Dictionary<string, int> TimeUnits = new Dictionary<string, int>()
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
            if (this.Context.Guild == null) return;
            var delTask = this.Context.Message.DeleteAsync();

            await this.Context.Guild.AddBanAsync(user);
            await delTask;
            await this.ReplyAsync($"User {user.Username} has been banned by {this.Context.User.Mention}.");
        }

        [Command("kick")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user)
        {
            var delTask = this.Context.Message.DeleteAsync();

            var kickTask = user.KickAsync();
            await delTask;
            if (kickTask == null) return;
            await kickTask;
            await this.ReplyAsync($"User {user.Username} has been kicked by {this.Context.User.Mention}.");
        }

        [Command("mute")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Mute(IGuildUser user, int amount = 0, string unitName = "")
        {
            var delTask = this.Context.Message.DeleteAsync();

            IRole mutedRole = this.Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.AddRolesAsync(mutedRole);
            await delTask;

            if (amount == 0)
            {
                await this.ReplyAsync($"User {user.Mention} has been muted by {this.Context.User.Mention}.");
            }
            else
            {
                var duration = amount * TimeUnits[unitName];
                await this.ReplyAsync($"User {user.Mention} has been muted by {this.Context.User.Mention} for {amount} {unitName}.");
                await Task.Delay(duration);
                await user.RemoveRolesAsync(mutedRole);
                await this.ReplyAsync($"User <@{user.Id}> has been unmuted automatically.");
            }
        }

        [Command("unmute")]
        [RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Unmute(IGuildUser user)
        {
            var delTask = this.Context.Message.DeleteAsync();
            IRole mutedRole = this.Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.RemoveRolesAsync(mutedRole);
            await delTask;
            await this.ReplyAsync($"User {user.Mention} has been unmuted by {this.Context.User.Mention}.");
        }

        [Group("stop")]
        public class StopModule : ModuleBase<SocketCommandContext>
        {
            [Command("on")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task On()
            {
                var channel = this.Context.Channel as IGuildChannel;

                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been restricted.").WithColor(new Color(244, 67, 54));
                await this.ReplyAsync(string.Empty, embed: embed.Build());
                await channel.AddPermissionOverwriteAsync(this.Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(this.Context.User, new OverwritePermissions(sendMessages: PermValue.Allow));
            }

            [Command("off")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Off()
            {
                var channel = this.Context.Channel as IGuildChannel;

                await channel.AddPermissionOverwriteAsync(this.Context.User, new OverwritePermissions(sendMessages: PermValue.Inherit));
                await channel.AddPermissionOverwriteAsync(this.Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Inherit));
                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been unrestricted.").WithColor(new Color(139, 195, 74));
                await this.ReplyAsync(string.Empty, embed: embed.Build());
            }
        }

        [Group("imagespam")]
        public class ImageSpamModule : ModuleBase<SocketCommandContext>
        {
            private ModerationService service;

            public ImageSpamModule(ModerationService svc)
            {
                this.service = svc;
            }

            [Command("block")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Block()
            {
                var channel = this.Context.Channel as ITextChannel;
                if (this.service.AddBinding(channel))
                {
                    await this.ReplyAsync($"Image spam in this channel (#{channel.Name}) is now blocked.");
                }
                else
                {
                    await this.ReplyAsync("Image spam in this channel is already blocked.");
                }
            }

            [Command("unblock")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Unblock()
            {
                var channel = this.Context.Channel as ITextChannel;
                if (this.service.RemoveBinding(channel))
                {
                    await this.ReplyAsync($"Image spam in this channel (#{channel.Name}) is no longer blocked.");
                }
                else
                {
                    await this.ReplyAsync("Image spam in this channel was not blocked.");
                }
            }

            [Command("check")]
            public async Task Check()
            {
                var channel = this.Context.Channel as ITextChannel;
                if (this.service.CheckBinding(channel))
                {
                    await this.ReplyAsync(
                        "Image spam in this channel is blocked. Send more than three images within 15 seconds will get you muted.");
                }
                else
                {
                    await this.ReplyAsync("Image spam in this channel is not restricted.");
                }
            }
        }
    }
}
