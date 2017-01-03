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
                using (var streamReader = new StreamReader(new FileStream("docs.txt", FileMode.Open)))
                {
                    string docsString = streamReader.ReadToEnd().Replace("[PREFIX]", Shinoa.Config["command_prefix"]);
                    string separator = Environment.NewLine + Environment.NewLine;

                    foreach (var segment in docsString.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        e.User.SendMessage(segment);
                    }

                    if (!e.Channel.IsPrivate) e.Channel.SendMessage($"<@{e.User.Id}> Check your PMs.");
                }
            });
        }
    }
}
