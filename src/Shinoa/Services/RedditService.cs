using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Shinoa.Attributes;
using Shinoa.Modules;
using SQLite;

namespace Shinoa.Services
{
    [Config("reddit")]
    public class RedditService : IService
    {
        public class RedditBinding
        {
            [PrimaryKey]
            public string SubredditName { get; set; }

            public DateTimeOffset LatestPost { get; set; }
        }

        //TODO: Migrate to Microsoft.EntityFrameworkCore.SQLite
        public class RedditChannelBinding
        {
            [Indexed]
            public string SubredditName { get; set; }

            [Indexed]
            public string ChannelId { get; set; }
        }

        class SubscribedSubreddit
        {
            public string Name;
            public ICollection<IMessageChannel> Channels = new List<IMessageChannel>();
            public DateTimeOffset LatestPost = DateTimeOffset.UtcNow;
        }
        
        private Timer refreshTimer;
        private SQLiteConnection db;
        private DiscordSocketClient client;
        private HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://www.reddit.com/r/")};
        private string[] compactKeywords, filterKeywords;

        public Color ModuleColor { get; private set; }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if(!map.TryGet(out db)) db = new SQLiteConnection(config["db_path"]);
            db.CreateTable<RedditBinding>();
            db.CreateTable<RedditChannelBinding>();
            client = map.Get<DiscordSocketClient>();

            compactKeywords = ((List<object>)config["compact_keywords"]).Cast<string>().ToArray();
            filterKeywords = ((List<object>)config["filter_keywords"]).Cast<string>().ToArray();

            ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));

            refreshTimer = new Timer(Callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(int.Parse((string)config["refresh_rate"])));
        }

        void Callback(object state)
        {
            foreach (var subreddit in GetFromDb())
            {
                var responseText = httpClient.HttpGet($"{subreddit.Name}/new/.json");
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);
                dynamic posts = responseObject["data"]["children"];
                var newestCreationTime = DateTimeOffset.FromUnixTimeSeconds((int) posts[0]["data"]["created_utc"]);
                Stack<Embed> postStack = new Stack<Embed>();

                foreach (var post in posts)
                {
                    var creationTime = DateTimeOffset.FromUnixTimeSeconds((int) post["data"]["created_utc"]);
                    if (creationTime <= subreddit.LatestPost)
                        break;

                    var title = System.Net.WebUtility.HtmlDecode((string) post["data"]["title"]);
                    string username = post["data"]["author"];
                    string id = post["data"]["id"];
                    string url = post["data"]["url"];
                    string selftext = post["data"]["selftext"];

                    if(filterKeywords.Any(kw => title.ToLower().Contains(kw))) continue;

                    bool compact = compactKeywords.Any(kw => title.ToLower().Contains(kw));

                    string imageUrl = null;
                    try
                    {
                        imageUrl = post["data"]["preview"]["images"][0]["source"]["url"];
                    }
                    catch (Exception) { }

                    string thumbnailUrl = null;
                    if (post["data"]["thumbnail"] != "self" && post["data"]["thumbnail"] != "default") thumbnailUrl = post["data"]["thumbnail"];

                    var embed = new EmbedBuilder()
                        .AddField(f => f.WithName("Title").WithValue(title))
                        .AddField(f => f.WithName("Submitted By").WithValue($"/u/{username}").WithIsInline(true))
                        .AddField(f => f.WithName("Subreddit").WithValue($"/r/{subreddit.Name}").WithIsInline(true))
                        .AddField(f => f.WithName("Shortlink").WithValue($"http://redd.it/{id}").WithIsInline(true))
                        .WithColor(ModuleColor);

                    if (!url.Contains("reddit.com")) embed.AddField(f => f.WithName("URL").WithValue(url));

                    if (!compact)
                    {
                        if (imageUrl != null)
                        {
                            embed.ImageUrl = imageUrl;
                        }
                        else if (thumbnailUrl != null)
                        {
                            embed.ThumbnailUrl = thumbnailUrl;
                        }

                        if (selftext != "") embed.AddField(f => f.WithName("Text").WithValue(selftext.Truncate(500)));
                    }
                    
                    if (!(compact || imageUrl == null))
                    {
                        var sauce = SauceModule.GetSauce(imageUrl);
                        if (sauce.SimilarityPercentage > 90)  postStack.Push(sauce.GetEmbed());
                    }
                    postStack.Push(embed.Build());
                }

                foreach (var embed in postStack)
                foreach (var channel in subreddit.Channels)
                {
                    channel.SendEmbedAsync(embed).Wait();
                }

                if (newestCreationTime > subreddit.LatestPost) subreddit.LatestPost = newestCreationTime;

                db.Update(new RedditBinding
                {
                    SubredditName = subreddit.Name,
                    LatestPost = subreddit.LatestPost
                });
            }
        }

        IEnumerable<SubscribedSubreddit> GetFromDb()
        {
            List<SubscribedSubreddit> ret = new List<SubscribedSubreddit>();
            foreach (var binding in db.Table<RedditBinding>())
            {
                var tmpSub = new SubscribedSubreddit
                {
                    Name = binding.SubredditName,
                    LatestPost = binding.LatestPost
                };
                foreach (var channelBinding in db.Table<RedditChannelBinding>().Where(b => b.SubredditName == tmpSub.Name))
                {
                    var tmpChannel = client.GetChannel(ulong.Parse(channelBinding.ChannelId)) as IMessageChannel;
                    if(tmpChannel == null) continue;
                    tmpSub.Channels.Add(tmpChannel);
                }
                ret.Add(tmpSub);
            }
            return ret;
        } 

        public bool AddBinding(string subredditName, IMessageChannel channel)
        {
            var name = subredditName.ToLower();

            if (db.Table<RedditChannelBinding>()
                    .Any(b => b.ChannelId == channel.Id.ToString() && b.SubredditName == name)) return false;

            if (db.Table<RedditBinding>().All(b => b.SubredditName != name))
            {
                db.Insert(new RedditBinding
                {
                    SubredditName = name,
                    LatestPost = DateTimeOffset.UtcNow
                });
            }

            db.Insert(new RedditChannelBinding
            {
                SubredditName = name,
                ChannelId = channel.Id.ToString()
            });
            return true;
        }

        public bool RemoveBinding(string subredditName, IMessageChannel channel)
        {
            var name = subredditName.ToLower();
            var idString = channel.Id.ToString();
            
            var found = db.Table<RedditChannelBinding>()
                        .Delete(b => b.ChannelId == idString && b.SubredditName == name) != 0;
            if (!found) return false;

            if (db.Table<RedditChannelBinding>().All(b => b.SubredditName != name))
                db.Delete(new RedditBinding
                {
                    SubredditName = name
                });
            return true;
        }

        public IEnumerable<RedditChannelBinding> GetBindings(IMessageChannel channel)
        {
            var idString = channel.Id.ToString();
            return db.Table<RedditChannelBinding>().Where(b => b.ChannelId == idString);
        }
    }
}
