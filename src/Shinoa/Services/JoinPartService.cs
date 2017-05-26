// <copyright file="JoinPartService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using SQLite;
    using System;

    public class JoinPartService : IDatabaseService
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

            if (db.Table<JoinPartServer>().Any(b => b.ServerId == binding.ServerId) && !move) return false;
            if (move) db.Update(binding);
            else db.Insert(binding);
            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var bindingId = binding.Id.ToString();
            if (db.Table<JoinPartServer>().All(b => b.ChannelId != bindingId)) return false;
            var serverIds = db.Table<JoinPartServer>()
                .Where(b => b.ChannelId == bindingId)
                .Select(server => server.ServerId).ToList();
            foreach (var server in serverIds)
            {
                db.Delete<JoinPartServer>(server);
            }

            return true;
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            if (!map.TryGet(out db)) db = new SQLiteConnection(config["db_path"]);
            db.CreateTable<JoinPartServer>();
            client = map.Get<DiscordSocketClient>();

            client.UserJoined += async user =>
            {
                await SendGreetingAsync(user.Guild, $"Welcome to the server, {user.Mention}!");
            };

            client.UserLeft += async user =>
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

        private IMessageChannel GetGreetingChannel(IGuild guild)
        {
            var server = Enumerable.FirstOrDefault(db.Table<JoinPartServer>(), srv => srv.ServerId == guild.Id.ToString());
            if (server == default(JoinPartServer)) return null;
            var greetingChannel = client.GetChannel(ulong.Parse(server.ChannelId));

            if (greetingChannel != null) return greetingChannel as IMessageChannel;
            db.Delete<JoinPartServer>(new JoinPartServer { ServerId = server.ServerId });
            return null;
        }

        private async Task SendGreetingAsync(IGuild guild, string message)
        {
            var channel = GetGreetingChannel(guild);
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
