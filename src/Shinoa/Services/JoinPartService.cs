// <copyright file="JoinPartService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Databases;

    using Discord;
    using Discord.WebSocket;

    using static Databases.JoinPartServerContext;

    public class JoinPartService : IDatabaseService
    {
        private JoinPartServerContext db;
        private DiscordSocketClient client;

        public bool AddBinding(IGuild guild, IMessageChannel channel, bool move = false)
        {
            var binding = new JoinPartServerBinding
            {
                ServerId = guild.Id,
                ChannelId = channel.Id,
            };

            if (db.DbSet.Any(b => b.ServerId == binding.ServerId) && !move) return false;
            if (move) db.Update(binding);
            else db.Add(binding);

            db.SaveChanges();
            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var servers = db.DbSet
                .Where(b => b.ChannelId == binding.Id);

            if (servers.Count() == 0) return false;

            db.RemoveRange(servers);
            db.SaveChanges();

            return true;
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(JoinPartServerContext)) as JoinPartServerContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

            var client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

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
            var server = db.DbSet.FirstOrDefault(srv => srv.ServerId == guild.Id);
            if (server == default(JoinPartServerBinding)) return null;
            var greetingChannel = client.GetChannel(server.ChannelId);

            if (greetingChannel != null) return greetingChannel as IMessageChannel;
            db.Remove(new JoinPartServerBinding { ServerId = server.ServerId });
            db.SaveChanges();
            return null;
        }

        private async Task SendGreetingAsync(IGuild guild, string message)
        {
            var channel = GetGreetingChannel(guild);
            if (channel == null) return;
            await channel.SendMessageAsync(message);
        }

        Task IDatabaseService.Callback() => db.SaveChangesAsync();
    }
}
