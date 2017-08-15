// <copyright file="JoinPartService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
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

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    using static Databases.JoinPartServerContext;

    public class JoinPartService : IDatabaseService
    {
        private DiscordSocketClient client;

        public async Task<bool> AddBinding(IGuild guild, IMessageChannel channel, bool move = false)
        {
            using (var db = Shinoa.Provider.GetService<JoinPartServerContext>())
            {
                var binding = new JoinPartServerBinding
                {
                    ServerId = guild.Id,
                    ChannelId = channel.Id,
                };

                var bindingExists = db.JoinPartServerBindings.Any(b => b.ServerId == binding.ServerId);

                if (bindingExists && !move) return false;
                if (bindingExists) db.JoinPartServerBindings.Update(binding);
                else db.JoinPartServerBindings.Add(binding);

                await db.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = Shinoa.Provider.GetService<JoinPartServerContext>())
            {
                var entities = db.JoinPartServerBindings.Where(b => b.ChannelId == binding.Id);

                if (!entities.Any()) return false;

                db.JoinPartServerBindings.RemoveRange(entities);
                await db.SaveChangesAsync();
                return true;
            }
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

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

        private async Task<IMessageChannel> GetGreetingChannel(IGuild guild)
        {
            using (var db = Shinoa.Provider.GetService<JoinPartServerContext>())
            {
                var server = await db.JoinPartServerBindings.FirstOrDefaultAsync(srv => srv.ServerId == guild.Id);
                if (server == default(JoinPartServerBinding)) return null;
                var greetingChannel = client.GetChannel(server.ChannelId);

                if (greetingChannel is IMessageChannel msgChannel) return msgChannel;
                db.JoinPartServerBindings.Remove(server);

                await db.SaveChangesAsync();
                return null;
            }
        }

        private async Task SendGreetingAsync(IGuild guild, string message)
        {
            var channel = await GetGreetingChannel(guild);
            if (channel == null) return;
            await channel.SendMessageAsync(message);
        }
    }
}
