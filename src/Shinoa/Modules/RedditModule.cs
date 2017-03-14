// <copyright file="RedditModule.cs" company="The Shinoa Development Team">
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

    [Group("reddit")]
    public class RedditModule : ModuleBase<SocketCommandContext>
    {
        private readonly RedditService service;

        public RedditModule(RedditService svc)
        {
            service = svc;
        }

        [Command("add")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string subredditName)
        {
            if (service.AddBinding(subredditName, Context.Channel))
            {
                await ReplyAsync($"Notifications for /r/{subredditName} have been bound to this channel (#{Context.Channel.Name}).");
            }
            else
            {
                await ReplyAsync($"Notifications for /r/{subredditName} are already bound to this channel (#{Context.Channel.Name}).");
            }
        }

        [Command("remove")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(string subredditName)
        {
            if (service.RemoveBinding(subredditName, Context.Channel))
            {
                await ReplyAsync($"Notifications for /r/{subredditName} have been unbound from this channel (#{Context.Channel.Name}).");
            }
            else
            {
                await ReplyAsync($"Notifications for /r/{subredditName} are not currently bound to this channel (#{Context.Channel.Name}).");
            }
        }

        [Command("list")]
        public async Task List()
        {
            var response = service.GetBindings(Context.Channel)
                        .Aggregate(string.Empty, (current, binding) => current + $"/r/{binding.SubredditName}\n");

            if (response == string.Empty) response = "N/A";

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Subreddits currently bound to this channel").WithValue(response))
                .WithColor(service.ModuleColor);

            await ReplyAsync(string.Empty, embed: embed.Build());
        }
    }
}