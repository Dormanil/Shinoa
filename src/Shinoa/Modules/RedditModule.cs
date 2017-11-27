// <copyright file="RedditModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Linq;
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;
    using Services;
    using Services.TimedServices;

    /// <summary>
    /// Module for reddit services.
    /// </summary>
    [Group("reddit")]
    [RequireNotBlacklisted]
    public class RedditModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public RedditService Service { get; set; }

        /// <summary>
        /// Command to add reddit notifications to the channel.
        /// </summary>
        /// <param name="subredditName">Name of the subreddit to add.</param>
        /// <returns></returns>
        [Command("add")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string subredditName)
        {
            subredditName = subredditName.Replace("r/", string.Empty).TrimStart('/'); // Extract name

            switch (await Service.AddBinding(subredditName, Context.Channel))
            {
                case BindingStatus.Success:
                    await ReplyAsync(
                        $"Notifications for /r/{subredditName} have been bound to this channel (#{Context.Channel.Name}).");
                    break;
                case BindingStatus.PreconditionFailed:
                    await ReplyAsync(
                        $"Notifications for /r/{subredditName} are already bound to this channel (#{Context.Channel.Name}).");
                    break;
                case BindingStatus.Error:
                    await ReplyAsync($"Could not access subreddit /r/{subredditName}. Does it exist?");
                    break;
            }
        }

        /// <summary>
        /// Command to remove reddit notifications from the channel.
        /// </summary>
        /// <param name="subredditName">Name of the subreddit to remove.</param>
        /// <returns></returns>
        [Command("remove")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(string subredditName)
        {
            subredditName = subredditName.Replace("r/", string.Empty).TrimStart('/');

            if (await Service.RemoveBinding(subredditName, Context.Channel))
            {
                await ReplyAsync($"Notifications for /r/{subredditName} have been unbound from this channel (#{Context.Channel.Name}).");
            }
            else
            {
                await ReplyAsync($"Notifications for /r/{subredditName} are not currently bound to this channel (#{Context.Channel.Name}).");
            }
        }

        /// <summary>
        /// Command to list the subreddits bound to the channel.
        /// </summary>
        /// <returns></returns>
        [Command("list")]
        public async Task List()
        {
            var response = Service.GetBindings(Context.Channel)
                        .Aggregate(string.Empty, (current, binding) => current + $"/r/{binding.Subreddit.SubredditName}\n");

            if (response == string.Empty) response = "N/A";

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Subreddits currently bound to this channel").WithValue(response))
                .WithColor(Service.ModuleColor);

            await ReplyAsync(string.Empty, embed: embed.Build());
        }
    }
}