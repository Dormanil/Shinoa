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
            this.BoundCommands.Add("help", (e) =>
            {
                e.Channel.SendMessage($"<@{e.User.Id}> <http://omegavesko.github.io/Shinoa/commands.html>");
            });
        }
    }
}
