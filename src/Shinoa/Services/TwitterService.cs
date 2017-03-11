using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SQLite;
using BoxKite.Twitter;
using BoxKite.Twitter.Models;
using Shinoa.Attributes;

namespace Shinoa.Services
{
    [Config("twitter")]
    public class TwitterService : IService
    {
        class TwitterBinding
        {
            [PrimaryKey]
            public string TwitterUsername { get; set; }
            
            public DateTimeOffset LatestPost { get; set; }
        }

        //TODO: Migrate to Microsoft.EntityFrameworkCore.SQLite
        public class TwitterChannelBinding
        {
            [Indexed]
            public string TwitterUsername { get; set; }

            [Indexed]
            public string ChannelId { get; set; }
        }

        class SubscribedUser
        {
            public string Username;
            public ICollection<IMessageChannel> Channels = new List<IMessageChannel>();
            public DateTimeOffset LatestPost = DateTimeOffset.UtcNow;
        }

        private Timer refreshTimer;
        private SQLiteConnection db;
        private DiscordSocketClient client;
        private ApplicationSession twitterSession;
        
        public Color ModuleColor { get; private set; }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if(!map.TryGet(out db)) db = new SQLiteConnection(config["db_path"]);
            db.CreateTable<TwitterBinding>();
            db.CreateTable<TwitterChannelBinding>();

            client = map.Get<DiscordSocketClient>();
            
            ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));

            twitterSession = new ApplicationSession(config["client_key"], config["client_secret"]);

            refreshTimer = new Timer(Callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(int.Parse((string)config["refresh_rate"])));
        }

        void Callback(object state)
        {
            foreach (var user in GetFromDb())
            {
                var response = twitterSession.GetUserTimeline(user.Username).Result;
                var newestCreationTime = response.FirstOrDefault()?.Time ?? DateTimeOffset.FromUnixTimeSeconds(0);
                Stack<Embed> postStack = new Stack<Embed>();

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
                foreach (var channel in user.Channels)
                {
                    channel.SendEmbedAsync(embed).Wait();
                }

                db.Update(new TwitterBinding
                {
                    TwitterUsername = user.Username,
                    LatestPost = user.LatestPost
                });
            }
        }

        IEnumerable<SubscribedUser> GetFromDb()
        {
            List<SubscribedUser> ret = new List<SubscribedUser>();
            foreach (var binding in db.Table<TwitterBinding>())
            {
                var tmpSub = new SubscribedUser
                {
                    Username = binding.TwitterUsername,
                    LatestPost = binding.LatestPost
                };
                foreach (var channelBinding in db.Table<TwitterChannelBinding>().Where(b => b.TwitterUsername == tmpSub.Username))
                {
                    var tmpChannel = client.GetChannel(ulong.Parse(channelBinding.ChannelId)) as IMessageChannel;
                    if(tmpChannel == null) continue;
                    tmpSub.Channels.Add(tmpChannel);
                }
                ret.Add(tmpSub);
            }
            return ret;
        }

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
                    LatestPost = DateTimeOffset.UtcNow
                });
            }

            db.Insert(new TwitterChannelBinding
            {
                TwitterUsername = name,
                ChannelId = channel.Id.ToString()
            });
            return true;
        }

        public bool RemoveBinding(string username, IMessageChannel channel)
        {
            var name = username.ToLower();
            var idString = channel.Id.ToString();

            var found =  db.Table<TwitterChannelBinding>()
                .Delete(b => b.ChannelId == idString && b.TwitterUsername == name) != 0;
            if (!found) return false;

            if (db.Table<TwitterChannelBinding>().All(b => b.TwitterUsername != name))
                db.Delete(new TwitterBinding
                {
                    TwitterUsername = name
                });
            return true;
        }

        public IEnumerable<TwitterChannelBinding> GetBindings(IMessageChannel channel)
        {
            var idString = channel.Id.ToString();
            return db.Table<TwitterChannelBinding>().Where(b => b.ChannelId == idString);
        }
    }
}
