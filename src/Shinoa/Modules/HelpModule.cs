// <copyright file="HelpModule.cs" company="The Shinoa Development Team">
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

    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpMessage()
        {
            var embed = new EmbedBuilder()
                    .AddField(f => f.WithName("Command List").WithValue("http://dormanil.github.io/Shinoa/commands.html"))
                    .AddField(f => f.WithName("GitHub").WithValue("https://github.com/Dormanil/Shinoa"))
                    .WithFooter(f => f.WithText(Shinoa.VersionString));

            await this.Context.Channel.SendMessageAsync($"{this.Context.User.Mention}", embed: embed.Build());
        }
    }
}
