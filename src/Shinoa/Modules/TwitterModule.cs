// <copyright file="TwitterModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Linq;
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;
    using Services.TimedServices;

    [Group("twitter")]
    public class TwitterModule : ModuleBase<SocketCommandContext>
    {
        private readonly TwitterService service;

        public TwitterModule(TwitterService svc)
        {
            service = svc;
        }

        [Command("add")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string user)
        {
            var twitterName = user.Replace("@", string.Empty).ToLower().Trim();
            if (service.AddBinding(twitterName, Context.Channel))
            {
                this.TryReplyAsync($"Notifications for @{twitterName} have been bound to this channel (#{Context.Channel.Name}).", out var replyTask);
                await replyTask;
            }
            else
            {
                this.TryReplyAsync($"Notifications for @{twitterName} are already bound to this channel (#{Context.Channel.Name}).", out var replyTask);
                await replyTask;
            }
        }

        [Command("remove")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(string user)
        {
            var twitterName = user.Replace("@", string.Empty).ToLower().Trim();
            if (service.RemoveBinding(twitterName, Context.Channel))
            {
                this.TryReplyAsync($"Notifications for @{twitterName} have been unbound from this channel (#{Context.Channel.Name}).", out var replyTask);
                await replyTask;
            }
            else
            {
                this.TryReplyAsync($"Notifications for @{twitterName} are not currently bound to this channel (#{Context.Channel.Name}).", out var replyTask);
                await replyTask;
            }
        }

        [Command("list")]
        public async Task List()
        {
            var response = service.GetBindings(Context.Channel)
                        .Aggregate(string.Empty, (current, binding) => current + $"@{binding.TwitterUsername}\n");

            if (response == string.Empty) response = "N/A";

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Twitter users currently bound to this channel").WithValue(response))
                .WithColor(service.ModuleColor);

            this.TryReplyAsync(string.Empty, out var replyTask, embed: embed.Build());
            await replyTask;
        }
    }
}
