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
        static Dictionary<ulong, ChatterBotSession> Sessions = new Dictionary<ulong, ChatterBotSession>();

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
                if (Convenience.ContainsBotMention(e.Message))
                {
                    var cleanMessage = Convenience.RemoveMentions(e.Message.RawText);

                    if (Sessions.ContainsKey(e.User.Id))
                    {
                        e.Channel.SendMessage($"<@{e.User.Id}> {Sessions[e.User.Id].Think(cleanMessage)}");
                    }
                    else
                    {
                        var factory = new ChatterBotFactory();
                        var bot = factory.Create(ChatterBotType.CLEVERBOT);
                        var session = bot.CreateSession();

                        Sessions.Add(e.User.Id, session);
                        e.Channel.SendMessage($"<@{e.User.Id}> {Sessions[e.User.Id].Think(cleanMessage)}");
                    }
                }
            }
        }
    }
}
