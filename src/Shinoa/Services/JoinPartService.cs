using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SQLite;

namespace Shinoa.Services
{
    public class JoinPartService : IService
    {
        public class JoinPartServer
        {
            [PrimaryKey]
            public string ServerId { get; set; }
            public string ChannelId { get; set; }
        }

        private SQLiteConnection db;
        private DiscordSocketClient client;

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if(!map.TryGet(out db)) db = new SQLiteConnection(config["db_path"]);
            db.CreateTable<JoinPartServer>();
            client = map.Get<DiscordSocketClient>();

            client.UserJoined += async (user) =>
            {
                Logging.Log($"User \"{user.Username}\" joined server \"{user.Guild.Name}\"");
                await SendGreetingAsync(user.Guild, $"Welcome to the server, {user.Mention}!");
            };

            client.UserLeft += async (user) =>
            {
                await SendGreetingAsync(user.Guild, $"{user.Mention} has left the server.");
            };

            client.UserBanned += async (user, guild) =>
            {
                await SendGreetingAsync(guild, $"{user.Mention} has been banned.");
            };

            client.UserUnbanned += async (user, guild) =>
            {
                await SendGreetingAsync(guild, $"{user.Mention} has been unbanned.");
            };
        }

        IMessageChannel GetGreetingChannel(IGuild guild)
        {
            Logging.Log($"Scanning database for ID \"{guild.Id}\"");
            JoinPartServer server = Enumerable.FirstOrDefault(db.Table<JoinPartServer>(), srv => srv.ServerId == guild.Id.ToString());
            if (server == default(JoinPartServer)) return null;
            var greetingChannel = client.GetChannel(ulong.Parse(server.ChannelId));

            if (greetingChannel != null) return greetingChannel as IMessageChannel;
            db.Delete<JoinPartServer>(new JoinPartServer {ServerId = server.ServerId});
            return null;
        }

        async Task SendGreetingAsync(IGuild guild, string message)
        {
            Logging.Log("Looking for greeting channel.");
            var channel = GetGreetingChannel(guild);
            if(channel == null) return;
            Logging.Log($"Sending message \"{message}\" to channel \"{channel.Name}\" on server \"{guild.Name}\"");
            await channel.SendMessageAsync(message);
        }

        public bool AddBinding(IGuild guild, IMessageChannel channel, bool move = false)
        {
            var binding = new JoinPartServer
            {
                ServerId = guild.Id.ToString(),
                ChannelId = channel.Id.ToString()
            };

            if (db.Table<JoinPartServer>().Any(b => b.ServerId == binding.ServerId) && !move) return false;
            if (move) db.Update(binding);
            else db.Insert(binding);
            return true;
        }

        public bool RemoveBinding(IGuild guild)
        {
            if (db.Table<JoinPartServer>().All(b => b.ServerId != guild.Id.ToString())) return false;
            db.Delete(new JoinPartServer {ServerId = guild.Id.ToString()});
            return true;
        }
    }
}
