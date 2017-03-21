// <copyright file="JoinPartModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Services;

    /// <summary>
    /// Module for setting up greetings and parting messages.
    /// </summary>
    [Group("greetings")]
    [Alias("joins", "welcome", "welcomes")]
    public class JoinPartModule : ModuleBase<SocketCommandContext>
    {
        private readonly JoinPartService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoinPartModule"/> class.
        /// </summary>
        /// <param name="svc">Backing service instance.</param>
        public JoinPartModule(JoinPartService svc)
        {
            service = svc;
        }

        /// <summary>
        /// Command to enable greetings in this channel.
        /// </summary>
        /// <returns></returns>
        [Command("enable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Enable()
        {
            if (service.AddBinding(Context.Guild, Context.Channel))
            {
                this.TryReplyAsync($"Greetings enabled for this server and bound to channel #{Context.Channel.Name}.", out var replyTask);
                await replyTask;
            }
            else
            {
                this.TryReplyAsync("Greetings are already enabled for this server. Did you mean to use `here`?", out var replyTask);
                await replyTask;
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
            if (service.RemoveBinding(Context.Guild))
            {
                this.TryReplyAsync("Greetings disabled for this server.", out var replyTask);
                await replyTask;
            }
            else
            {
                this.TryReplyAsync("Greetings aren't enabled for this server.", out var replyTask);
                await replyTask;
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
            if (service.AddBinding(Context.Guild, Context.Channel, true))
            {
                this.TryReplyAsync($"Greetings moved to channel #{Context.Channel.Name}.", out var replyTask);
                await replyTask;
            }
            else
            {
                this.TryReplyAsync("Greetings aren't enabled for this server.", out var replyTask);
                await replyTask;
            }
        }
    }
}
