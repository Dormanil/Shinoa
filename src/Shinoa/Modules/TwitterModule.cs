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
    using Services;
    using Services.TimedServices;

    [Group("twitter")]
    [RequireNotBlacklisted]
    public class TwitterModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public TwitterService Service { get; set; }

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
