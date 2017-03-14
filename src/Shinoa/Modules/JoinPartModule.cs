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

    [Group("greetings")]
    [Alias("joins", "welcome", "welcomes")]
    public class JoinPartModule : ModuleBase<SocketCommandContext>
    {
        private readonly JoinPartService service;

        public JoinPartModule(JoinPartService svc)
        {
            this.service = svc;
        }

        [Command("enable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Enable()
        {
            if (this.service.AddBinding(this.Context.Guild, this.Context.Channel))
            {
                await this.ReplyAsync($"Greetings enabled for this server and bound to channel #{this.Context.Channel.Name}.");
            }
            else
            {
                await this.ReplyAsync("Greetings are already enabled for this server. Did you mean to use `here`?");
            }
        }

        [Command("disable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Disable()
        {
            if (this.service.RemoveBinding(this.Context.Guild))
            {
                await this.ReplyAsync("Greetings disabled for this server.");
            }
            else
            {
                await this.ReplyAsync("Greetings aren't enabled for this server.");
            }
        }

        [Command("here")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Here()
        {
            if (this.service.AddBinding(this.Context.Guild, this.Context.Channel, true))
            {
                await this.ReplyAsync($"Greetings moved to channel #{this.Context.Channel.Name}.");
            }
            else
            {
                await this.ReplyAsync("Greetings aren't enabled for this server.");
            }
        }
    }
}
