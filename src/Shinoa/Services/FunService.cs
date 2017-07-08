// <copyright file="FunService.cs" company="The Shinoa Development Team">
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
    using static Databases.BotFunctionSpamContext;

    public class FunService : IDatabaseService
    {
        private DbContextOptions dbOptions;

        public async Task<bool> AddBinding(IMessageChannel channel)
        {
            using (var db = new BotFunctionSpamContext(dbOptions))
            {
                if (db.BotFunctionSpamBindings.Any(b => b.ChannelId == channel.Id)) return false;

                db.BotFunctionSpamBindings.Add(new BotFunctionSpamBinding
                {
                    ChannelId = channel.Id,
                });
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = new BotFunctionSpamContext(dbOptions))
            {
                var entities = db.BotFunctionSpamBindings.Where(b => b.ChannelId == binding.Id);
                if (!entities.Any()) return false;

                db.BotFunctionSpamBindings.RemoveRange(entities);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public bool CheckBinding(IMessageChannel channel)
        {
            using (var db = new BotFunctionSpamContext(dbOptions))
            return db.BotFunctionSpamBindings.All(b => b.ChannelId != channel.Id);
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            dbOptions = map.GetService(typeof(DbContextOptions)) as DbContextOptions ?? throw new ServiceNotFoundException("Database Options not found in service provider.");

            var client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");
            client.MessageReceived += MessageReceivedHandler;
        }

        private async Task MessageReceivedHandler(SocketMessage m)
        {
            if (m.Author.IsBot) return;
            if (CheckBinding(m.Channel)) return;

            switch (m.Content)
            {
                case @"o/":
                    await m.Channel.SendMessageAsync(@"\o");
                    break;
                case @"\o":
                    await m.Channel.SendMessageAsync(@"o/");
                    break;
                case @"/o/":
                    await m.Channel.SendMessageAsync(@"\o\");
                    break;
                case @"\o\":
                    await m.Channel.SendMessageAsync(@"/o/");
                    break;
                default:
                    if (m.Content.ToLower() == "wake me up")
                    {
                        await m.Channel.SendMessageAsync($"_Wakes {(m.Author is IGuildUser author ? author.Nickname : m.Author.Username)} up inside._");
                    }
                    else if (m.Content.ToLower().StartsWith("wake") && m.Content.ToLower().EndsWith("up"))
                    {
                        var messageArray = m.Content.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                            .Skip(1)
                            .Reverse()
                            .Skip(1)
                            .Reverse();

                        await m.Channel.SendMessageAsync($"_Wakes {messageArray.Aggregate(string.Empty, (current, word) => current + word + " ").Trim()} up inside._");
                    }

                    break;
            }
        }
    }
}
