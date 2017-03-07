using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using SQLite;
using Discord.WebSocket;
using Discord.Commands;

namespace Shinoa.Modules
{
    //TODO: Improve migrate
    public class JoinPartModule : ModuleBase<SocketCommandContext>
    {
        class JoinPartServer
        {
            [PrimaryKey]
            public string ServerId { get; set; }
            public string ChannelId { get; set; }
        }

        static bool init = false;

        List<Guid> Guilds = new List<Guid>();

        public JoinPartModule(DiscordSocketClient client)
        {
            if (!init) return;
            Shinoa.DatabaseConnection.CreateTable<JoinPartServer>();

            client.UserJoined += async (user) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) != user.Guild.Id) continue;
                    var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                    await greetingChannel.SendMessageAsync($"Welcome to the server, {user.Mention}!");
                    break;
                }
            };

            client.UserLeft += async (user) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) != user.Guild.Id) continue;
                    var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                    await greetingChannel.SendMessageAsync($"{user.Mention} has left the server.");
                    break;
                }
            };

            client.UserBanned += async (user, guild) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) != guild.Id) continue;
                    var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                    await greetingChannel.SendMessageAsync($"{user.Mention} has been banned.");
                    break;
                }
            };

            client.UserUnbanned += async (user, guild) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) != guild.Id) continue;
                    var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                    await greetingChannel.SendMessageAsync($"{user.Mention} has been unbanned.");
                    break;
                }
            };
            init = true;
        }

        [Command("greetings"), Alias("joins", "welcome", "welcomes"), RequireUserPermission(GuildPermission.ManageGuild)]
        public async Task GreetingsManagement(string option)
        {
            var serverIdString = Context.Guild.Id.ToString();
            
            switch (option)
            {
                case "enable":
                    if (Shinoa.DatabaseConnection.Table<JoinPartServer>().All(s => s.ServerId != serverIdString))
                    {

                        Shinoa.DatabaseConnection.Insert(new JoinPartServer() { ServerId = serverIdString, ChannelId = Context.Channel.Id.ToString() });
                        await ReplyAsync($"Greetings enabled for this server and bound to channel #{Context.Channel.Name}.");
                    }
                    else
                    {
                        await ReplyAsync("Greetings are already enabled for this server. Did you mean to use `here`?");
                    }
                    break;

                case "disable":
                    if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Count(s => s.ServerId == serverIdString) == 1)
                    {
                        Shinoa.DatabaseConnection.Delete(new JoinPartServer() { ServerId = serverIdString });
                        await ReplyAsync($"Greetings disabled for this server.");
                    }
                    else
                    {
                        await ReplyAsync("Greetings aren't enabled for this server.");
                    }
                    break;

                case "here":
                    if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Count(s => s.ServerId == serverIdString) == 1)
                    {
                        Shinoa.DatabaseConnection.Update(new JoinPartServer() { ServerId = serverIdString, ChannelId = Context.Channel.Id.ToString() });
                        ReplyAsync($"Greetings moved to channel #{Context.Channel.Name}.");
                    }
                    else
                    {
                        ReplyAsync("Greetings aren't enabled for this server.");
                    }
                    break;
            }
        }
    }
}
