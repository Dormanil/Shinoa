// <copyright file="TwitterModule.cs" company="The Shinoa Development Team">
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
    /// Command group to add, remove and list twitter subscriptions for channels.
    /// </summary>
    [Group("twitter")]
    [RequireNotBlacklisted]
    public class TwitterModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public TwitterService Service { get; set; }

        /// <summary>
        /// Adds a twitter feed to the channel.
        /// </summary>
        /// <param name="user">User to follow.</param>
        /// <returns></returns>
        [Command("add")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string user)
        {
            var twitterName = user.TrimStart('@');

            switch (await Service.AddBinding(twitterName, Context.Channel))
            {
                case BindingStatus.Error:
                    await ReplyAsync($"Could not access @{twitterName}. Does the handle exist?");
                    break;
                case BindingStatus.AlreadyExists:
                    await ReplyAsync($"Notifications for @{twitterName} are already bound to this channel (#{Context.Channel.Name}).");
                    break;
                case BindingStatus.Added:
                    await ReplyAsync($"Notifications for @{twitterName} have been bound to this channel (#{Context.Channel.Name}).");
                    break;
            }
        }

        /// <summary>
        /// Removes a twitter feed from the channel.
        /// </summary>
        /// <param name="user">User to unfollow.</param>
        /// <returns></returns>
        [Command("remove")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(string user)
        {
            var twitterName = user.TrimStart('@');
            if (await Service.RemoveBinding(twitterName, Context.Channel))
            {
                await ReplyAsync($"Notifications for @{twitterName} have been unbound from this channel (#{Context.Channel.Name}).");
            }
            else
            {
                await ReplyAsync($"Notifications for @{twitterName} are not currently bound to this channel (#{Context.Channel.Name}).");
            }
        }

        /// <summary>
        /// Lists all twitter feed in this channel.
        /// </summary>
        /// <returns></returns>
        [Command("list")]
        public async Task List()
        {
            var response = Service.GetBindings(Context.Channel)
                        .Aggregate(string.Empty, (current, binding) => current + $"@{binding.TwitterBinding.TwitterUsername}\n");

            if (response == string.Empty) response = "N/A";

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Twitter users currently bound to this channel").WithValue(response))
                .WithColor(Service.ModuleColor);

            await ReplyAsync(string.Empty, embed: embed.Build());
        }
    }
}
