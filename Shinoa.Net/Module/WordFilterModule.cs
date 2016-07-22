using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Shinoa.Net.Module
{
    class WordFilterModule : IModule
    {
        class Server
        {
            public ulong id;
            public Channel notificationChannel;
            public List<Channel> ignoredChannels = new List<Channel>();
            public List<string> wordList = new List<string>();
        }

        List<Server> ServerList = new List<Server>();

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            foreach (var server in ShinoaNet.Config["wordfilter"])
            {
                var listEntry = new Server();
                listEntry.id = ulong.Parse(server["server_id"]);
                listEntry.notificationChannel = ShinoaNet.DiscordClient.GetChannel(ulong.Parse(server["notification_channel"]));

                foreach (var word in server["words"]) listEntry.wordList.Add(word);
                foreach (var id in server["ignore_channels"]) listEntry.ignoredChannels.Add(ShinoaNet.DiscordClient.GetChannel(ulong.Parse(id)));

                ServerList.Add(listEntry);
            }
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id &&
                e.User.Id != ulong.Parse(ShinoaNet.Config["owner_id"]))
            {
                foreach(var server in ServerList)
                {
                    if (server.id == e.Server.Id)
                    {
                        foreach (var word in server.wordList)
                        {
                            if (e.Message.Text.Contains(word))
                            {
                                foreach (var channel in server.ignoredChannels)
                                {
                                    if (e.Channel.Id == channel.Id) return;
                                }

                                server.notificationChannel.SendMessage(
                                    $"Message from user '{e.User.Name}' in #{e.Channel.Name} contains the word '{word}':\n```\n{e.Message.Text}\n```");

                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
