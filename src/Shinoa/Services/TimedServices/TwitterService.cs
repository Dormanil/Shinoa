// <copyright file="TwitterService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
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
    using Discord.Commands;
    using Discord.WebSocket;

    using static Databases.TwitterContext;

    [Config("twitter")]
    public class TwitterService : IDatabaseService, ITimedService
    {
        private TwitterContext db;
        private DiscordSocketClient client;
        private ApplicationSession twitterSession;

        public Color ModuleColor { get; private set; }

        public bool AddBinding(string username, IMessageChannel channel)
        {
            var twitterBinding = new TwitterBinding
            {
                TwitterUsername = username,
                LatestPost = DateTime.UtcNow,
            };

            if (db.DbSet.Any(b => b.ChannelId == channel.Id && b.TwitterBinding.TwitterUsername == twitterBinding.TwitterUsername)) return false;

            db.DbSet.Add(new TwitterChannelBinding
            {
                TwitterBinding = twitterBinding,
                ChannelId = channel.Id,
            });
            return true;
        }

        public bool RemoveBinding(string username, IMessageChannel channel)
        {
            var name = username.ToLower();

            var found = db.DbSet.FirstOrDefault(b => b.ChannelId == channel.Id && b.TwitterBinding.TwitterUsername == name);
            if (found == default(TwitterChannelBinding)) return false;

            db.Remove(found);
            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var found = db.DbSet.FirstOrDefault(b => b.ChannelId == binding.Id);
            if (found == default(TwitterChannelBinding)) return false;

            db.Remove(found);
            return true;
        }

        public IEnumerable<TwitterChannelBinding> GetBindings(IMessageChannel channel)
        {
            return db.DbSet.Where(b => b.ChannelId == channel.Id);
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(TwitterContext)) as TwitterContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

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

        Task IDatabaseService.Callback() => db.SaveChangesAsync();

        async Task ITimedService.Callback()
        {
            foreach (var user in GetFromDb())
            {
                var response = await twitterSession.GetUserTimeline(user.Username);
                var newestCreationTime = response.FirstOrDefault()?.Time ?? DateTimeOffset.FromUnixTimeSeconds(0);
                var postStack = new Stack<Embed>();

                foreach (var tweet in response)
                {
                    if (tweet.Time <= user.LatestPost) break;
                    user.LatestPost = tweet.Time.DateTime;

                    var embed = new EmbedBuilder()
                        .WithUrl($"https://twitter.com/{tweet.User.ScreenName}/status/{tweet.Id}")
                        .WithDescription(tweet.Text)
                        .WithThumbnailUrl(tweet.User.Avatar)
                        .WithColor(ModuleColor);

                    embed.Title = tweet.IsARetweet() ? $"{tweet.RetweetedStatus.User.Name} (retweeted by @{tweet.User.ScreenName})" : $"{tweet.User.Name} (@{tweet.User.ScreenName})";

                    postStack.Push(embed.Build());
                }

                if (newestCreationTime > user.LatestPost) user.LatestPost = newestCreationTime.DateTime;

                foreach (var embed in postStack)
                {
                    foreach (var channel in user.Channels)
                    {
                        await channel.SendEmbedAsync(embed);
                    }
                }

                db.Update(new TwitterBinding
                {
                    TwitterUsername = user.Username,
                    LatestPost = user.LatestPost,
                });
            }
        }

        private IEnumerable<SubscribedUser> GetFromDb()
        {
            var ret = new List<SubscribedUser>();
            foreach (var binding in db.TwitterBindingSet)
            {
                var tmpSub = new SubscribedUser
                {
                    Username = binding.TwitterUsername,
                    LatestPost = binding.LatestPost,
                };
                foreach (var channelBinding in db.DbSet.Where(b => b.TwitterBinding.TwitterUsername == tmpSub.Username))
                {
                    var tmpChannel = client.GetChannel(channelBinding.ChannelId) as IMessageChannel;
                    if (tmpChannel == null) continue;
                    tmpSub.Channels.Add(tmpChannel);
                }

                ret.Add(tmpSub);
            }

            return ret;
        }

        private class SubscribedUser
        {
            public string Username { get; set; }

            public ICollection<IMessageChannel> Channels { get; } = new List<IMessageChannel>();

            public DateTime LatestPost { get; set; } = DateTime.UtcNow;
        }
    }
}
