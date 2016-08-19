using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

using ChatterBotAPI;
using System.Text.RegularExpressions;

namespace Shinoa.Net.Module
{
    class CleverbotModule : IModule
    {
        static ChatterBotSession CleverbotSession;

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            var factory = new ChatterBotFactory();
            var bot = factory.Create(ChatterBotType.CLEVERBOT);
            CleverbotSession = bot.CreateSession();
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                if (Convenience.ContainsBotMention(e.Message))
                {
                    var cleanMessage = Convenience.RemoveMentions(e.Message.RawText);
                    e.Channel.SendMessage(CleverbotSession.Think(cleanMessage));
                }
            }
        }
    }
}
