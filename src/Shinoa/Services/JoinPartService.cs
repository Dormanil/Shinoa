// <copyright file="JoinPartService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System.Linq;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using SQLite;

    public class JoinPartService : IService
    {
        private SQLiteConnection db;
        private DiscordSocketClient client;

        public bool AddBinding(IGuild guild, IMessageChannel channel, bool move = false)
        {
            var binding = new JoinPartServer
            {
                ServerId = guild.Id.ToString(),
                ChannelId = channel.Id.ToString(),
            };

            if (this.db.Table<JoinPartServer>().Any(b => b.ServerId == binding.ServerId) && !move) return false;
            if (move) this.db.Update(binding);
            else this.db.Insert(binding);
            return true;
        }

        public bool RemoveBinding(IGuild guild)
        {
            if (this.db.Table<JoinPartServer>().All(b => b.ServerId != guild.Id.ToString())) return false;
            this.db.Delete(new JoinPartServer { ServerId = guild.Id.ToString() });
            return true;
        }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if (!map.TryGet(out this.db)) this.db = new SQLiteConnection(config["db_path"]);
            this.db.CreateTable<JoinPartServer>();
            this.client = map.Get<DiscordSocketClient>();

            this.client.UserJoined += async (user) =>
            {
                await this.SendGreetingAsync(user.Guild, $"Welcome to the server, {user.Mention}!");
            };

            this.client.UserLeft += async (user) =>
            {
                await this.SendGreetingAsync(user.Guild, $"{user.Mention} has left the server.");
            };

            this.client.UserBanned += async (user, guild) =>
            {
                await this.SendGreetingAsync(guild, $"{user.Mention} has been banned.");
            };

            this.client.UserUnbanned += async (user, guild) =>
            {
                await this.SendGreetingAsync(guild, $"{user.Mention} has been unbanned.");
            };
        }

        private IMessageChannel GetGreetingChannel(IGuild guild)
        {
            var server = Enumerable.FirstOrDefault(this.db.Table<JoinPartServer>(), srv => srv.ServerId == guild.Id.ToString());
            if (server == default(JoinPartServer)) return null;
            var greetingChannel = this.client.GetChannel(ulong.Parse(server.ChannelId));

            if (greetingChannel != null) return greetingChannel as IMessageChannel;
            this.db.Delete<JoinPartServer>(new JoinPartServer { ServerId = server.ServerId });
            return null;
        }

        private async Task SendGreetingAsync(IGuild guild, string message)
        {
            var channel = this.GetGreetingChannel(guild);
            if (channel == null) return;
            await channel.SendMessageAsync(message);
        }

        public class JoinPartServer
        {
            [PrimaryKey]
            public string ServerId { get; set; }

            public string ChannelId { get; set; }
        }
    }
}
