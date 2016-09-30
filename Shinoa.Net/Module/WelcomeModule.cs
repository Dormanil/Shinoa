using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Shinoa.Net.Module
{
    class WelcomeModule : IModule
    {
        List<Server> servers = new List<Server>();

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            foreach (string serverId in ShinoaNet.Config["welcome_servers"])
            {
                servers.Add(ShinoaNet.DiscordClient.GetServer(ulong.Parse(serverId)));
            }

            ShinoaNet.DiscordClient.UserJoined += (s, e) =>
            {
                var send = false;
                foreach(var server in servers)
                {
                    if (server.Id == e.Server.Id)
                    {
                        send = true;
                        break;
                    }
                }

                if (send) e.Server.DefaultChannel.SendMessage($"Welcome to the server, <@{e.User.Id}>!");
            };
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {            
        }
    }
}
