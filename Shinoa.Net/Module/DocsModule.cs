using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;
using System.Text.RegularExpressions;

namespace Shinoa.Net.Module
{
    class DocsModule : IModule
    {
        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"help");
                if (regex.IsMatch(e.Message.Text))
                {
                    Logging.LogMessage(e.Message);

                    using (var streamReader = new StreamReader("docs.txt"))
                    {
                        e.Channel.SendMessage(streamReader.ReadToEnd().Replace("[PREFIX]", ShinoaNet.Config["command_prefix"]));
                    }
                }
            }
        }
    }
}
