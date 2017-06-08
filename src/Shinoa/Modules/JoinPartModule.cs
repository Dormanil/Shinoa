// <copyright file="JoinPartModule.cs" company="The Shinoa Development Team">
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
    using Services;

    /// <summary>
    /// Module for setting up greetings and parting messages.
    /// </summary>
    [Group("greetings")]
    [Alias("joins", "welcome", "welcomes")]
    [RequireNotBlacklisted]
    public class JoinPartModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public JoinPartService Service { get; set; }

        /// <summary>
        /// Command to enable greetings in this channel.
        /// </summary>
        /// <returns></returns>
        [Command("enable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Enable()
        {
            if (await Service.AddBinding(Context.Guild, Context.Channel))
            {
                await ReplyAsync($"Greetings enabled for this server and bound to channel <#{Context.Channel.Id}>.");
            }
            else
            {
                await ReplyAsync("Greetings are already enabled for this server. Did you mean to use `here`?");
            }
        }

        /// <summary>
        /// Command to disable greetings in this channel.
        /// </summary>
        /// <returns></returns>
        [Command("disable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Disable()
        {
            if (await Service.RemoveBinding(Context.Guild))
            {
                await ReplyAsync("Greetings disabled for this server.");
            }
            else
            {
                await ReplyAsync("Greetings aren't enabled for this server.");
            }
        }

        /// <summary>
        /// Command to move greetings to this channel.
        /// </summary>
        /// <returns></returns>
        [Command("here")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Here()
        {
            if (await Service.AddBinding(Context.Guild, Context.Channel, true))
            {
                await ReplyAsync($"Greetings moved to channel <#{Context.Channel.Id}>.");
            }
            else
            {
                await ReplyAsync("Greetings aren't enabled for this server.");
            }
        }
    }
}
