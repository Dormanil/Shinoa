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
    using Discord;
    using Discord.Commands;
    using Discord.Net;
    using Discord.WebSocket;
    using SQLite;

    public class ModerationService : IService
    {
        private SQLiteConnection db;

        public bool AddBinding(ITextChannel channel)
        {
            if (this.db.Table<ImageSpamBinding>().Any(b => b.ChannelId == channel.Id.ToString())) return false;

            this.db.Insert(new ImageSpamBinding
            {
                ChannelId = channel.Id.ToString(),
            });
            return true;
        }

        public bool RemoveBinding(ITextChannel channel)
        {
            return this.db.Delete<ImageSpamBinding>(channel.Id.ToString()) != 0;
        }

        public bool CheckBinding(ITextChannel channel)
        {
            return this.db.Table<ImageSpamBinding>().Any(b => b.ChannelId == channel.Id.ToString());
        }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if (!map.TryGet(out this.db)) this.db = new SQLiteConnection(config["db_path"]);
            this.db.CreateTable<ImageSpamBinding>();

            var client = map.Get<DiscordSocketClient>();
            client.MessageReceived += this.Handler;
        }

        private async Task Handler(SocketMessage msg)
        {
            try
            {
                if (msg.Author is IGuildUser user &&
                    this.db.Table<ImageSpamBinding>().Any(b => b.ChannelId == msg.Channel.Id.ToString()) &&
                    msg.Attachments.Count > 0)
                {
                    var imagesCounter = 0;
                    var messages = await msg.Channel.GetMessagesAsync(limit: 50).Flatten();
                    foreach (var message in messages.ToList().OrderByDescending(o => o.Timestamp))
                    {
                        var timeDifference = DateTimeOffset.Now - message.Timestamp;
                        if (timeDifference.TotalSeconds < 15 && message.Attachments.Count > 0 &&
                            message.Author.Id == msg.Author.Id)
                        {
                            imagesCounter++;
                        }
                    }

                    if (imagesCounter > 2)
                    {
                        await msg.DeleteAsync();
                        await msg.Channel.SendMessageAsync(
                            $"{user.Mention} Your message has been removed for being image spam. You have been preventively muted.");

                        var mutedRole = user.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

                        await user.AddRolesAsync(mutedRole);
                        await Task.Delay(5 * 60 * 1000);
                        await user.RemoveRolesAsync(mutedRole);
                        await msg.Channel.SendMessageAsync(
                            $"User {user.Mention} has been unmuted automatically.");
                    }
                }
            }
            catch (HttpException)
            {
                return;
            }
        }

        private class ImageSpamBinding
        {
            [PrimaryKey]
            public string ChannelId { get; set; }
        }
    }
}
