using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using SQLite;
using Discord.WebSocket;
using Discord.Commands;
using Shinoa.Attributes;

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

        List<Guid> Guilds = new List<Guid>();

        public override void Init()
        {
            Shinoa.DatabaseConnection.CreateTable<JoinPartServer>();

            Shinoa.DiscordClient.UserJoined += async (user) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) == user.Guild.Id)
                    {
                        var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                        await greetingChannel.SendMessageAsync($"Welcome to the server, {user.Mention}!");
                        break;
                    }
                }
            };

            Shinoa.DiscordClient.UserLeft += async (user) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) == user.Guild.Id)
                    {
                        var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                        await greetingChannel.SendMessageAsync($"{user.Mention} has left the server.");
                        break;
                    }
                }
            };

            Shinoa.DiscordClient.UserBanned += async (user, guild) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) == guild.Id)
                    {
                        var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                        await greetingChannel.SendMessageAsync($"{user.Mention} has been banned.");
                        break;
                    }
                }
            };

            Shinoa.DiscordClient.UserUnbanned += async (user, guild) =>
            {
                foreach (var server in Shinoa.DatabaseConnection.Table<JoinPartServer>())
                {
                    if (ulong.Parse(server.ServerId) == guild.Id)
                    {
                        var greetingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(server.ChannelId)) as IMessageChannel;
                        await greetingChannel.SendMessageAsync($"{user.Mention} has been unbanned.");
                        break;
                    }
                }
            };
        }

        [@Command("greetings", "joins", "welcome", "welcomes")]
        public async Task GreetingsManagement(CommandContext c, params string[] args)
        {
            var serverIdString = c.Guild.Id.ToString();

            if ((c.User as SocketGuildUser).GuildPermissions.ManageGuild)
            {
                switch (args[0])
                {
                    case "enable":
                        if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Where(s => s.ServerId == serverIdString).Count() == 0)
                        {

                            Shinoa.DatabaseConnection.Insert(new JoinPartServer() { ServerId = serverIdString, ChannelId = c.Channel.Id.ToString() });
                            await c.Channel.SendMessageAsync($"Greetings enabled for this server and bound to channel #{c.Channel.Name}.");
                        }
                        else
                        {
                            await c.Channel.SendMessageAsync("Greetings are already enabled for this server. Did you mean to use `here`?");
                        }
                        break;

                    case "disable":
                        if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Where(s => s.ServerId == serverIdString).Count() == 1)
                        {
                            Shinoa.DatabaseConnection.Delete(new JoinPartServer() { ServerId = serverIdString });
                            await c.Channel.SendMessageAsync($"Greetings disabled for this server.");
                        }
                        else
                        {
                            await c.Channel.SendMessageAsync("Greetings aren't enabled for this server.");
                        }
                        break;

                    case "here":
                        if (Shinoa.DatabaseConnection.Table<JoinPartServer>().Where(s => s.ServerId == serverIdString).Count() == 1)
                        {
                            Shinoa.DatabaseConnection.Update(new JoinPartServer() { ServerId = serverIdString, ChannelId = c.Channel.Id.ToString() });
                            await c.Channel.SendMessageAsync($"Greetings moved to channel #{c.Channel.Name}.");
                        }
                        else
                        {
                            await c.Channel.SendMessageAsync("Greetings aren't enabled for this server.");
                        }
                        break;
                }
            }
            else
            {
                await c.Channel.SendPermissionErrorAsync("Manage Server");
            }
        }
    }
}
