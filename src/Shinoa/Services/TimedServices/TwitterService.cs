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
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using SQLite;

    [Config("twitter")]
    public class TwitterService : IDatabaseService, ITimedService
    {
        private SQLiteConnection db;
        private DiscordSocketClient client;
        private ApplicationSession twitterSession;

        public Color ModuleColor { get; private set; }

        public bool AddBinding(string username, IMessageChannel channel)
        {
            var name = username.ToLower();

            if (db.Table<TwitterChannelBinding>()
                    .Any(b => b.ChannelId == channel.Id.ToString() && b.TwitterUsername == name)) return false;

            if (db.Table<TwitterBinding>().All(b => b.TwitterUsername != name))
            {
                db.Insert(new TwitterBinding
                {
                    TwitterUsername = name,
                    LatestPost = DateTimeOffset.UtcNow,
                });
            }

            db.Insert(new TwitterChannelBinding
            {
                TwitterUsername = name,
                ChannelId = channel.Id.ToString(),
            });
            return true;
        }

        public bool RemoveBinding(string username, IMessageChannel channel)
        {
            var name = username.ToLower();
            var idString = channel.Id.ToString();

            var found = db.Table<TwitterChannelBinding>()
                .Delete(b => b.ChannelId == idString && b.TwitterUsername == name) != 0;
            if (!found) return false;

            if (db.Table<TwitterChannelBinding>().All(b => b.TwitterUsername != name))
            {
                db.Delete(new TwitterBinding
                {
                    TwitterUsername = name,
                });
            }

            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var usernames = db.Table<TwitterChannelBinding>()
                .Where(b => b.ChannelId == binding.Id.ToString())
                .Select(b => b.TwitterUsername);

            var found = db.Table<TwitterChannelBinding>().Delete(b => b.ChannelId == binding.Id.ToString()) != 0;
            if (!found) return false;

            foreach (var username in usernames)
            {
                if (db.Table<TwitterChannelBinding>().All(b => b.TwitterUsername != username))
                {
                    db.Delete(new TwitterBinding
                    {
                        TwitterUsername = username,
                    });
                }
            }

            return true;
        }

        public IEnumerable<TwitterChannelBinding> GetBindings(IMessageChannel channel)
        {
            var idString = channel.Id.ToString();
            return db.Table<TwitterChannelBinding>().Where(b => b.ChannelId == idString);
        }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if (!map.TryGet(out db)) db = new SQLiteConnection(config["db_path"]);
            db.CreateTable<TwitterBinding>();
            db.CreateTable<TwitterChannelBinding>();

            client = map.Get<DiscordSocketClient>();

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
            foreach (var user in GetFromDb())
            {
                var response = await twitterSession.GetUserTimeline(user.Username);
                var newestCreationTime = response.FirstOrDefault()?.Time ?? DateTimeOffset.FromUnixTimeSeconds(0);
                var postStack = new Stack<Embed>();

                foreach (var tweet in response)
                {
                    if (tweet.Time <= user.LatestPost) break;
                    user.LatestPost = tweet.Time;

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
            foreach (var binding in db.Table<TwitterBinding>())
            {
                var tmpSub = new SubscribedUser
                {
                    Username = binding.TwitterUsername,
                    LatestPost = binding.LatestPost,
                };
                foreach (var channelBinding in db.Table<TwitterChannelBinding>().Where(b => b.TwitterUsername == tmpSub.Username))
                {
                    var tmpChannel = client.GetChannel(ulong.Parse(channelBinding.ChannelId)) as IMessageChannel;
                    if (tmpChannel == null) continue;
                    tmpSub.Channels.Add(tmpChannel);
                }

                ret.Add(tmpSub);
            }

            return ret;
        }

        // TODO: Migrate to Microsoft.EntityFrameworkCore.SQLite
        public class TwitterChannelBinding
        {
            [Indexed]
            public string TwitterUsername { get; set; }

            [Indexed]
            public string ChannelId { get; set; }
        }

        private class TwitterBinding
        {
            [PrimaryKey]
            public string TwitterUsername { get; set; }

            public DateTimeOffset LatestPost { get; set; }
        }

        private class SubscribedUser
        {
            public string Username { get; set; }

            public ICollection<IMessageChannel> Channels { get; } = new List<IMessageChannel>();

            public DateTimeOffset LatestPost { get; set; } = DateTimeOffset.UtcNow;
        }
    }
}
