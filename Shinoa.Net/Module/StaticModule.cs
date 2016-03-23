using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Shinoa.Net.Module
{
    class StaticModule : IModule
    {
        public void Init()
        {

        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {  
            if (Convenience.ContainsBotMention(e.Message.RawText))
            {
                var cleanMessage = Convenience.RemoveMentions(e.Message.RawText).Trim().ToLower();

                foreach(var staticMessage in ShinoaNet.Config["static_replies"])
                {
                    if (cleanMessage.Contains(staticMessage["trigger"]))
                    {
                        Logging.LogMessage(e.Message);

                        e.Channel.SendMessage($"<@{e.User.Id}> {staticMessage["reply"]}");
                        break;
                    }
                }
            }
        }
    }
}
