using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.IO;

namespace Shinoa.Modules
{
    public class HelpModule : Abstract.Module
    {
        public override void Init()
        {
            this.BoundCommands.Add("help", (c) =>
            {
                var embed = new EmbedBuilder()
                    .AddField(f => f.WithName("Command List").WithValue("http://omegavesko.github.io/Shinoa/commands.html"))
                    .AddField(f => f.WithName("GitHub").WithValue("https://github.com/omegavesko/Shinoa"))
                    .WithFooter(f => f.WithText(Shinoa.VersionString));

                c.Channel.SendMessageAsync($"{c.User.Mention}", embed: embed.Build());
            });
        }
    }
}
