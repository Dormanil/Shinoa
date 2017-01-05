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
                c.Channel.SendMessageAsync($"<@{c.User.Id}> <http://omegavesko.github.io/Shinoa/commands.html>");
            });
        }
    }
}
