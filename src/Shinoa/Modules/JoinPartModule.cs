using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using SQLite;

namespace Shinoa.Modules
{
    public class JoinPartModule : Abstract.Module
    {
        class JoinPartServer
        {
            [PrimaryKey]
            public string ServerId { get; set; }
            public string ChannelId { get; set; }
        }

        List<Server> Servers = new List<Server>();

        public override void Init()
        {
            Shinoa.DatabaseConnection.CreateTable<JoinPartServer>();

            this.BoundCommands.Add("greetings", (e) => 
            {
                var serverIdString = e.Server.Id.ToString();

                if (e.User.ServerPermissions.ManageServer)
                {
                    switch (GetCommandParameters(e.Message.Text)[0])
                    {
                        case "enable":
                            if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Where(s => s.ServerId == serverIdString).Count() == 0)
                            {

                                Shinoa.DatabaseConnection.Insert(new JoinPartServer() { ServerId = serverIdString, ChannelId = e.Channel.Id.ToString() });
                                e.Channel.SendMessage($"Greetings enabled for this server and bound to channel #{e.Channel.Name}.");
                            }
                            else
                            {
                                e.Channel.SendMessage("Greetings are already enabled for this server. Did you mean to use `here`?");
                            }
                            break;

                        case "disable":
                            if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Where(s => s.ServerId == serverIdString).Count() == 1)
                            {
                                Shinoa.DatabaseConnection.Delete(new JoinPartServer() { ServerId = serverIdString });
                                e.Channel.SendMessage($"Greetings disabled for this server.");
                            }
                            else
                            {
                                e.Channel.SendMessage("Greetings aren't enabled for this server.");
                            }
                            break;

                        case "here":
                            if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Where(s => s.ServerId == serverIdString).Count() == 1)
                            {
                                Shinoa.DatabaseConnection.Update(new JoinPartServer() { ServerId = serverIdString, ChannelId = e.Channel.Id.ToString() });
                                e.Channel.SendMessage($"Greetings moved to channel #{e.Channel.Name}.");
                            }
                            else
                            {
                                e.Channel.SendMessage("Greetings aren't enabled for this server.");
                            }
                            break;
                    }
                }
                else
                {
                    e.Channel.SendPermissionError("Manage Server");
                }

            });

            Shinoa.DiscordClient.UserJoined += (s, e) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) == e.Server.Id)
                    {
                        Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)).SendMessage($"Welcome to the server, <@{e.User.Id}>!");
                        break;
                    }
                }
            };

            Shinoa.DiscordClient.UserLeft += (s, e) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) == e.Server.Id)
                    {
                        Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)).SendMessage($"{e.User.Name} has left the server.");
                        break;
                    }
                }
            };
        }
    }
}
