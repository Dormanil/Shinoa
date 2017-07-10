// <copyright file="HelpModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Module to get help using the bot.
    /// </summary>
    [RequireNotBlacklisted]
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Command to print the help message.
        /// </summary>
        /// <param name="arg">Unwanted argument, the command won't do anything if there is one.</param>
        /// <returns></returns>
        [Command("help")]
        public async Task HelpMessage([Remainder]string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return;

            var embed = new EmbedBuilder()
                    .AddField(f => f.WithName("Command List").WithValue("http://dormanil.github.io/Shinoa/commands.html"))
                    .AddField(f => f.WithName("GitHub").WithValue("https://github.com/Dormanil/Shinoa"))
                    .WithFooter(f => f.WithText(Shinoa.VersionString));

            await Context.Channel.SendMessageAsync($"{Context.User.Mention}", embed: embed.Build());
        }
    }
}
