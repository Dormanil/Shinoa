using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        [Command("help")]
        public async Task HelpMessage()
        {
            var embed = new EmbedBuilder()
                    .AddField(f => f.WithName("Command List").WithValue("http://dormanil.github.io/Shinoa/commands.html"))
                    .AddField(f => f.WithName("GitHub").WithValue("https://github.com/Dormanil/Shinoa"))
                    .WithFooter(f => f.WithText(Shinoa.VersionString));

            await Context.Channel.SendMessageAsync($"{Context.User.Mention}", embed: embed.Build());
        }
    }
}
