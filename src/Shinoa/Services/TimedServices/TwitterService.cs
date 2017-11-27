// <copyright file="TwitterService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services.TimedServices
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Attributes;
    using BoxKite.Twitter;
    using BoxKite.Twitter.Models;
    using Databases;
    using Discord;
    using Discord.WebSocket;

    using Extensions;

    using Microsoft.EntityFrameworkCore;
    using static Databases.TwitterContext;

    [Config("twitter")]
    public class TwitterService : IDatabaseService, ITimedService
    {
        private DbContextOptions dbOptions;
        private DiscordSocketClient client;
        private ApplicationSession twitterSession;

        public Color ModuleColor { get; private set; }

        public async Task<BindingStatus> AddBinding(string username, IMessageChannel channel)
        {
            if (string.IsNullOrEmpty((await twitterSession.GetUserProfile(username)).Name)) return BindingStatus.Error;
            using (var db = new TwitterContext(dbOptions))
            {
                var twitterBinding = new TwitterBinding
                {
                    TwitterUsername = username.ToLower(),
                    LatestPost = DateTime.UtcNow,
                };

                if (db.TwitterChannelBindings.Any(b => b.ChannelId == channel.Id && b.TwitterBinding.TwitterUsername == twitterBinding.TwitterUsername)) return BindingStatus.PreconditionFailed;

                db.TwitterChannelBindings.Add(new TwitterChannelBinding
                {
                    TwitterBinding = twitterBinding,
                    ChannelId = channel.Id,
                });

                await db.SaveChangesAsync();
                return BindingStatus.Success;
            }
        }

        public async Task<bool> RemoveBinding(string username, IMessageChannel channel)
        {
            using (var db = new TwitterContext(dbOptions))
            {
                var name = username.ToLower();

                var found = await db.TwitterChannelBindings.FirstOrDefaultAsync(b => b.ChannelId == channel.Id && b.TwitterBinding.TwitterUsername == name);
                if (found == default(TwitterChannelBinding)) return false;

                db.TwitterChannelBindings.Remove(found);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = new TwitterContext(dbOptions))
            {
                var entities = db.TwitterChannelBindings.Where(b => b.ChannelId == binding.Id);
                if (!entities.Any()) return false;

                db.TwitterChannelBindings.RemoveRange(entities);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public IEnumerable<TwitterChannelBinding> GetBindings(IMessageChannel channel)
        {
            using (var db = new TwitterContext(dbOptions))
                return db.TwitterChannelBindings.Where(b => b.ChannelId == channel.Id).Include(b => b.TwitterBinding).ToList();
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            dbOptions = map.GetService(typeof(DbContextOptions)) as DbContextOptions ?? throw new ServiceNotFoundException("Database Options were not found in service provider.");

            client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

            ModuleColor = new Color(33, 155, 243);
            try
            {
                ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));
            }
            catch (KeyNotFoundException)
            {
                Logging.LogError(
                        "TwitterService.Init(): The property was not found on the dynamic object. No colors were supplied.")
                    .Wait();
            }
            catch (Exception e)
            {
                Logging.LogError(e.ToString()).Wait();
            }

            twitterSession = new ApplicationSession(config["client_key"], config["client_secret"]);
        }

        async Task ITimedService.Callback()
        {
            using (var db = new TwitterContext(dbOptions))
            {
                foreach (var user in db.TwitterBindings.Include(b => b.ChannelBindings))
                {
                    var response = await twitterSession.GetUserTimeline(user.TwitterUsername);
                    var newestCreationTime = response.FirstOrDefault()?.Time.DateTime ?? DateTime.UtcNow;
                    var postStack = new Stack<Embed>();

                    foreach (var tweet in response)
                    {
                        if (tweet.Time.DateTime <= user.LatestPost) break;

                        var embed = new EmbedBuilder()
                            .WithUrl($"https://twitter.com/{tweet.User.ScreenName}/status/{tweet.Id}")
                            .WithDescription(tweet.Text)
                            .WithThumbnailUrl(tweet.User.Avatar)
                            .WithColor(ModuleColor);

                        embed.Title = tweet.IsARetweet() ? $"{tweet.RetweetedStatus.User.Name} (retweeted by @{tweet.User.ScreenName})" : $"{tweet.User.Name} (@{tweet.User.ScreenName})";

                        postStack.Push(embed.Build());
                    }

                    if (newestCreationTime > user.LatestPost) user.LatestPost = newestCreationTime;

                    foreach (var embed in postStack)
                    {
                        foreach (var channelBinding in user.ChannelBindings)
                        {
                            if (client.GetChannel(channelBinding.ChannelId) is IMessageChannel channel) await channel.SendEmbedAsync(embed);
                        }
                    }

                    db.TwitterBindings.Update(user);
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
