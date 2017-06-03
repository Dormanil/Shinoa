// <copyright file="FunService.cs" company="The Shinoa Development Team">
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
    using static Databases.BotFunctionSpamContext;

    public class FunService : IDatabaseService
    {
        private BotFunctionSpamContext db;

        public bool AddBinding(IMessageChannel channel)
        {
            if (db.DbSet.Any(b => b.ChannelId == channel.Id)) return false;

            db.Add(new BotFunctionSpamBinding
            {
                ChannelId = channel.Id,
            });
            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var entity = db.DbSet.FirstOrDefault(b => b.ChannelId == binding.Id);

            if (entity == null)
                return false;

            db.Remove(entity);
            db.SaveChanges();
            return true;
        }

        public bool CheckBinding(IMessageChannel channel)
        {
            return db.DbSet.All(b => b.ChannelId != channel.Id);
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(BotFunctionSpamContext)) as BotFunctionSpamContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

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

        Task IDatabaseService.Callback() => db.SaveChangesAsync();
    }
}
