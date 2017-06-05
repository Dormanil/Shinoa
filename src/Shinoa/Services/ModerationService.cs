// <copyright file="ModerationService.cs" company="The Shinoa Development Team">
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
    using Discord.Net;
    using Discord.WebSocket;
    using static Databases.ImageSpamContext;

    public class ModerationService : IDatabaseService
    {
        private ImageSpamContext db;

        public bool AddBinding(IMessageChannel channel)
        {
            if (db.ImageSpamBindings.Any(b => b.ChannelId == channel.Id)) return false;

            db.Add(new ImageSpamBinding
            {
                ChannelId = channel.Id,
            });
            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var entities = db.ImageSpamBindings.Where(b => b.ChannelId == binding.Id);
            if (!entities.Any()) return false;

            db.ImageSpamBindings.RemoveRange(entities);
            return true;
        }

        public bool CheckBinding(IMessageChannel channel)
        {
            return db.ImageSpamBindings.Any(b => b.ChannelId == channel.Id);
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(ImageSpamContext)) as ImageSpamContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

            var client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");
            client.MessageReceived += Handler;
        }

        private async Task Handler(SocketMessage msg)
        {
            try
            {
                if (msg.Author is IGuildUser user &&
                    db.ImageSpamBindings.Any(b => b.ChannelId == msg.Channel.Id) &&
                    msg.Attachments.Count > 0)
                {
                    var messages = await msg.Channel.GetMessagesAsync(limit: 50).Flatten();
                    var imagesCounter = (from message in messages.ToList().OrderByDescending(o => o.Timestamp)
                                         let timeDifference = DateTimeOffset.Now - message.Timestamp
                                         where timeDifference.TotalSeconds < 15 && message.Attachments.Count > 0 && message.Author.Id == msg.Author.Id
                                         select message).Count();

                    if (imagesCounter > 2)
                    {
                        await msg.DeleteAsync();
                        await msg.Channel.SendMessageAsync($"{user.Mention} Your message has been removed for being image spam. You have been preventively muted.");

                        var mutedRole = user.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

                        await user.AddRoleAsync(mutedRole);
                        await Task.Delay(5 * 60 * 1000);
                        await user.RemoveRoleAsync(mutedRole);
                        await msg.Channel.SendMessageAsync($"User {user.Mention} has been unmuted automatically.");
                    }
                }
            }
            catch (HttpException)
            {
            }
        }

        Task IDatabaseService.Callback() => db.SaveChangesAsync();
    }
}
