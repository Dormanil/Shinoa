// <copyright file="AnimeFeedModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;
    using Services.TimedServices;

    /// <summary>
    /// Module for automatic updates on airing anime.
    /// </summary>
    [Group("animefeed")]
    public class AnimeFeedModule : ModuleBase<SocketCommandContext>
    {
        private readonly AnimeFeedService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimeFeedModule"/> class.
        /// </summary>
        /// <param name="svc">Backing service instance.</param>
        public AnimeFeedModule(AnimeFeedService svc)
        {
            service = svc;
        }

        /// <summary>
        /// Command to enable the anime feed for the current channel.
        /// </summary>
        /// <returns></returns>
        [Command("enable")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Enable()
        {
            if (service.AddBinding(Context.Channel))
            {
                if (!(Context.Channel is IPrivateChannel))
                    await ReplyAsync($"Anime notifications have been bound to this channel (#{Context.Channel.Name}).");
                else
                    await ReplyAsync("You will now receive anime notifications via PM.");
            }
            else
            {
                if (!(Context.Channel is IPrivateChannel))
                    await ReplyAsync($"Anime notifications are already bound to this channel (#{Context.Channel.Name}).");
                else
                    await ReplyAsync("You are already receiving anime notifications.");
            }
        }

        /// <summary>
        /// Command to disable the anime feed for the current channel.
        /// </summary>
        /// <returns></returns>
        [Command("disable")]
        [RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Disable()
        {
            if (service.RemoveBinding(Context.Channel))
            {
                if (!(Context.Channel is IPrivateChannel))
                    await ReplyAsync($"Anime notifications have been unbound from this channel (#{Context.Channel.Name}).");
                else
                    await ReplyAsync("You will no lonnger receive anime notifications.");
            }
            else
            {
                if (!(Context.Channel is IPrivateChannel))
                    await ReplyAsync($"Anime notifications are currently not bound to this channel (#{Context.Channel.Name}).");
                else
                    await ReplyAsync("You are currently not receiving anime notifications.");
            }
        }
    }
}
