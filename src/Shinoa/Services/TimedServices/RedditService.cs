// <copyright file="RedditService.cs" company="The Shinoa Development Team">
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
    using System.Net.Http;
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Modules;
    using Newtonsoft.Json;
    using SQLite;

    [Config("reddit")]
    public class RedditService : ITimedService
    {
        private readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://www.reddit.com/r/") };

        private SQLiteConnection db;
        private DiscordSocketClient client;
        private string[] compactKeywords;
        private string[] filterKeywords;

        public Color ModuleColor { get; private set; }

        public bool AddBinding(string subredditName, IMessageChannel channel)
        {
            var name = subredditName.ToLower();

            if (this.db.Table<RedditChannelBinding>()
                    .Any(b => b.ChannelId == channel.Id.ToString() && b.SubredditName == name)) return false;

            if (this.db.Table<RedditBinding>().All(b => b.SubredditName != name))
            {
                this.db.Insert(new RedditBinding
                {
                    SubredditName = name,
                    LatestPost = DateTimeOffset.UtcNow,
                });
            }

            this.db.Insert(new RedditChannelBinding
            {
                SubredditName = name,
                ChannelId = channel.Id.ToString(),
            });
            return true;
        }

        public bool RemoveBinding(string subredditName, IMessageChannel channel)
        {
            var name = subredditName.ToLower();
            var idString = channel.Id.ToString();

            var found = this.db.Table<RedditChannelBinding>()
                        .Delete(b => b.ChannelId == idString && b.SubredditName == name) != 0;
            if (!found) return false;

            if (this.db.Table<RedditChannelBinding>().All(b => b.SubredditName != name))
            {
                this.db.Delete(new RedditBinding
                {
                    SubredditName = name,
                });
            }

            return true;
        }

        public IEnumerable<RedditChannelBinding> GetBindings(IMessageChannel channel)
        {
            var idString = channel.Id.ToString();
            return this.db.Table<RedditChannelBinding>().Where(b => b.ChannelId == idString);
        }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if (!map.TryGet(out this.db)) this.db = new SQLiteConnection(config["db_path"]);
            this.db.CreateTable<RedditBinding>();
            this.db.CreateTable<RedditChannelBinding>();
            this.client = map.Get<DiscordSocketClient>();

            this.compactKeywords = ((List<object>)config["compact_keywords"]).Cast<string>().ToArray();
            this.filterKeywords = ((List<object>)config["filter_keywords"]).Cast<string>().ToArray();

            this.ModuleColor = new Color(255, 152, 0);
            try
            {
                this.ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));
            }
            catch (KeyNotFoundException)
            {
                Logging.LogError(
                        "RedditService.Init: The property was not found on the dynamic object. No colors were supplied.")
                    .Wait();
            }
            catch (Exception e)
            {
                Logging.LogError(e.ToString()).Wait();
            }
        }

        async Task ITimedService.Callback()
        {
            foreach (var subreddit in this.GetFromDb())
            {
                var responseText = this.httpClient.HttpGet($"{subreddit.Name}/new/.json");
                if (responseText == null) continue;

                dynamic responseObject = JsonConvert.DeserializeObject(responseText);
                dynamic posts = responseObject["data"]["children"];
                var newestCreationTime = DateTimeOffset.FromUnixTimeSeconds((int)posts[0]["data"]["created_utc"]);
                var postStack = new Stack<Embed>();

                foreach (var post in posts)
                {
                    var creationTime = DateTimeOffset.FromUnixTimeSeconds((int)post["data"]["created_utc"]);
                    if (creationTime <= subreddit.LatestPost)
                        break;

                    var title = System.Net.WebUtility.HtmlDecode((string)post["data"]["title"]);
                    string username = post["data"]["author"];
                    string id = post["data"]["id"];
                    string url = post["data"]["url"];
                    string selftext = post["data"]["selftext"];

                    if (this.filterKeywords.Any(kw => title.ToLower().Contains(kw))) continue;

                    var compact = this.compactKeywords.Any(kw => title.ToLower().Contains(kw));

                    string imageUrl = null;
                    try
                    {
                        imageUrl = post["data"]["preview"]["images"][0]["source"]["url"];
                    }
                    catch (Exception)
                    {
                    }

                    string thumbnailUrl = null;
                    if (post["data"]["thumbnail"] != "self" && post["data"]["thumbnail"] != "default") thumbnailUrl = post["data"]["thumbnail"];

                    var embed = new EmbedBuilder()
                        .AddField(f => f.WithName("Title").WithValue(title))
                        .AddField(f => f.WithName("Submitted By").WithValue($"/u/{username}").WithIsInline(true))
                        .AddField(f => f.WithName("Subreddit").WithValue($"/r/{subreddit.Name}").WithIsInline(true))
                        .AddField(f => f.WithName("Shortlink").WithValue($"http://redd.it/{id}").WithIsInline(true))
                        .WithColor(this.ModuleColor);

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

                        if (selftext != string.Empty) embed.AddField(f => f.WithName("Text").WithValue(selftext.Truncate(500)));
                    }

                    if (!(compact || imageUrl == null))
                    {
                        var sauce = SauceModule.GetSauce(imageUrl);
                        if (sauce.SimilarityPercentage > 90) postStack.Push(sauce.GetEmbed());
                    }

                    postStack.Push(embed.Build());
                }

                foreach (var embed in postStack)
                {
                    foreach (var channel in subreddit.Channels)
                    {
                        await channel.SendEmbedAsync(embed);
                    }
                }

                if (newestCreationTime > subreddit.LatestPost) subreddit.LatestPost = newestCreationTime;

                this.db.Update(new RedditBinding
                {
                    SubredditName = subreddit.Name,
                    LatestPost = subreddit.LatestPost,
                });
            }
        }

        private IEnumerable<SubscribedSubreddit> GetFromDb()
        {
            var ret = new List<SubscribedSubreddit>();
            foreach (var binding in this.db.Table<RedditBinding>())
            {
                var tmpSub = new SubscribedSubreddit
                {
                    Name = binding.SubredditName,
                    LatestPost = binding.LatestPost,
                };
                foreach (var channelBinding in this.db.Table<RedditChannelBinding>().Where(b => b.SubredditName == tmpSub.Name))
                {
                    var tmpChannel = this.client.GetChannel(ulong.Parse(channelBinding.ChannelId)) as IMessageChannel;
                    if (tmpChannel == null) continue;
                    tmpSub.Channels.Add(tmpChannel);
                }

                ret.Add(tmpSub);
            }

            return ret;
        }

        public class RedditBinding
        {
            [PrimaryKey]
            public string SubredditName { get; set; }

            public DateTimeOffset LatestPost { get; set; }
        }

        // TODO: Migrate to Microsoft.EntityFrameworkCore.SQLite
        public class RedditChannelBinding
        {
            [Indexed]
            public string SubredditName { get; set; }

            [Indexed]
            public string ChannelId { get; set; }
        }

        private class SubscribedSubreddit
        {
            public string Name { get; set; }

            public ICollection<IMessageChannel> Channels { get; } = new List<IMessageChannel>();

            public DateTimeOffset LatestPost { get; set; } = DateTimeOffset.UtcNow;
        }
    }
}
