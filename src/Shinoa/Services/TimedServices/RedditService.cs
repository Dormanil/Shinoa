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
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Attributes;

    using Databases;

    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    using Modules;

    using Newtonsoft.Json;

    using static Databases.RedditContext;

    [Config("reddit")]
    public class RedditService : IDatabaseService, ITimedService
    {
        private readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://www.reddit.com/r/") };

        private RedditContext db;
        private DiscordSocketClient client;
        private string[] compactKeywords;
        private string[] filterKeywords;

        public Color ModuleColor { get; private set; }

        public bool AddBinding(string subredditName, IMessageChannel channel)
        {
            var subreddit = new RedditBinding
            {
                SubredditName = subredditName.ToLower(),
                LatestPost = DateTime.UtcNow,
            };

            if (db.RedditChannelBindings.Any(b => b.ChannelId == channel.Id && b.Subreddit.SubredditName == subreddit.SubredditName)) return false;

            db.Add(new RedditChannelBinding
            {
                Subreddit = subreddit,
                ChannelId = channel.Id,
            });
            return true;
        }

        public bool RemoveBinding(string subredditName, IMessageChannel channel)
        {
            var name = new RedditBinding { SubredditName = subredditName.ToLower(), };

            var found = db.RedditChannelBindings.FirstOrDefault(b => b.ChannelId == channel.Id && b.Subreddit.SubredditName == name.SubredditName);
            if (found == default(RedditChannelBinding)) return false;

            db.Remove(found);
            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var entities = db.RedditChannelBindings.Where(b => b.ChannelId == binding.Id);
            if (!entities.Any()) return false;

            db.RedditChannelBindings.RemoveRange(entities);
            return true;
        }

        public IEnumerable<RedditChannelBinding> GetBindings(IMessageChannel channel)
        {
            return db.RedditChannelBindings.Where(b => b.ChannelId == channel.Id);
        }

        async void IService.Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(RedditContext)) as RedditContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

            client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

            compactKeywords = ((List<object>)config["compact_keywords"]).Cast<string>().ToArray();
            filterKeywords = ((List<object>)config["filter_keywords"]).Cast<string>().ToArray();

            ModuleColor = new Color(255, 152, 0);
            try
            {
                ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));
            }
            catch (KeyNotFoundException)
            {
                await Logging.LogError("RedditService.Init: The property was not found on the dynamic object. No colors were supplied.");
            }
            catch (Exception e)
            {
                await Logging.LogError(e.ToString());
            }
        }

        Task IDatabaseService.Callback() => db.SaveChangesAsync();

        async Task ITimedService.Callback()
        {
            foreach (var subreddit in GetFromDb())
            {
                var responseText = await httpClient.HttpGet($"{subreddit.Name}/new/.json");
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

                    var title = WebUtility.HtmlDecode((string)post["data"]["title"]);
                    string username = post["data"]["author"];
                    string id = post["data"]["id"];
                    string url = post["data"]["url"];
                    string selftext = post["data"]["selftext"];

                    if (filterKeywords.Any(kw => title.ToLower().Contains(kw))) continue;

                    var compact = compactKeywords.Any(kw => title.ToLower().Contains(kw));

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

                        if (selftext != string.Empty) embed.AddField(f => f.WithName("Text").WithValue(selftext.Truncate(500)));
                    }

                    if (!(compact || imageUrl == null))
                    {
                        try
                        {
                            var sauce = SauceModule.GetSauce(imageUrl);
                            if (sauce.SimilarityPercentage > 90) postStack.Push(sauce.GetEmbed());
                        }
                        catch (SauceModule.SauceNotFoundException sauceNotFoundException)
                        {
                            await Logging.LogError(sauceNotFoundException.ToString());
                        }
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

                db.Update(new RedditBinding
                {
                    SubredditName = subreddit.Name,
                    LatestPost = subreddit.LatestPost,
                });
            }
        }

        private IEnumerable<SubscribedSubreddit> GetFromDb()
        {
            var ret = new List<SubscribedSubreddit>();
            foreach (var binding in db.RedditBindings)
            {
                var tmpSub = new SubscribedSubreddit
                {
                    Name = binding.SubredditName,
                    LatestPost = binding.LatestPost,
                };
                foreach (var channelBinding in db.RedditChannelBindings.Where(b => b.Subreddit.SubredditName == tmpSub.Name))
                {
                    var tmpChannel = client.GetChannel(channelBinding.ChannelId) as IMessageChannel;
                    if (tmpChannel == null) continue;
                    tmpSub.Channels.Add(tmpChannel);
                }

                ret.Add(tmpSub);
            }

            return ret;
        }

        private class SubscribedSubreddit
        {
            public string Name { get; set; }

            public ICollection<IMessageChannel> Channels { get; } = new List<IMessageChannel>();

            public DateTimeOffset LatestPost { get; set; } = DateTimeOffset.UtcNow;
        }
    }
}
