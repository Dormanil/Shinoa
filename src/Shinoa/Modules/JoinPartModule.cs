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
            service = svc;
        }

        [Command("enable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Enable()
        {
            if (service.AddBinding(Context.Guild, Context.Channel))
            {
                await ReplyAsync($"Greetings enabled for this server and bound to channel #{Context.Channel.Name}.");
            }
            else
            {
                await ReplyAsync("Greetings are already enabled for this server. Did you mean to use `here`?");
            }
        }

        [Command("disable")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Disable()
        {
            if (service.RemoveBinding(Context.Guild))
            {
                await ReplyAsync("Greetings disabled for this server.");
            }
            else
            {
                await ReplyAsync("Greetings aren't enabled for this server.");
            }
        }

        [Command("here")]
        [RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task Here()
        {
            if (service.AddBinding(Context.Guild, Context.Channel, true))
            {
                await ReplyAsync($"Greetings moved to channel #{Context.Channel.Name}.");
            }
            else
            {
                await ReplyAsync("Greetings aren't enabled for this server.");
            }
        }
    }
}
