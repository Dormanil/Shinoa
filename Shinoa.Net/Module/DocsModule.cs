using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;

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
            if (Convenience.ContainsBotMention(e.Message.RawText))
            {
                if (Convenience.RemoveMentions(e.Message.RawText).Trim().ToLower().Equals("help"))
                {
                    Logging.LogMessage(e.Message);

                    using (var streamReader = new StreamReader("docs.txt"))
                    {
                        e.Channel.SendMessage(streamReader.ReadToEnd());
                    }
                }
            }
        }
    }
}
