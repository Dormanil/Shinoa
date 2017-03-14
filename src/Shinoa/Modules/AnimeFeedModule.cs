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

    [Group("animefeed")]
    public class AnimeFeedModule : ModuleBase<SocketCommandContext>
    {
        private AnimeFeedService service;

        public AnimeFeedModule(AnimeFeedService svc)
        {
            service = svc;
        }

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
