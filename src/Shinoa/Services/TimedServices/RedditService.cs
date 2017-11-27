// <copyright file="RedditService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
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
    using Discord.WebSocket;

    using Extensions;

    using Microsoft.EntityFrameworkCore;
    using Modules;
    using Newtonsoft.Json;
    using static Databases.RedditContext;

    [Config("reddit")]
    public class RedditService : IDatabaseService, ITimedService
    {
        private readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://www.reddit.com/r/") };

        private DbContextOptions dbOptions;
        private DiscordSocketClient client;
        private string[] compactKeywords;
        private string[] filterKeywords;

        public Color ModuleColor { get; private set; }

        public async Task<BindingStatus> AddBinding(string subredditName, IMessageChannel channel)
        {
            if (!(await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, subredditName))).IsSuccessStatusCode) return BindingStatus.Error;
            using (var db = new RedditContext(dbOptions))
            {
                var subreddit = new RedditBinding
                {
                    SubredditName = subredditName.ToLower(),
                    LatestPost = DateTime.UtcNow,
                };

                if (db.RedditChannelBindings.Any(b => b.ChannelId == channel.Id && b.Subreddit.SubredditName == subreddit.SubredditName)) return BindingStatus.PreconditionFailed;

                db.RedditChannelBindings.Add(new RedditChannelBinding
                {
                    Subreddit = subreddit,
                    ChannelId = channel.Id,
                });

                await db.SaveChangesAsync();
                return BindingStatus.Success;
            }
        }

        public async Task<bool> RemoveBinding(string subredditName, IMessageChannel channel)
        {
            using (var db = new RedditContext(dbOptions))
            {
                var name = subredditName.ToLower();

                var found = await db.RedditChannelBindings.FirstOrDefaultAsync(b => b.ChannelId == channel.Id && b.Subreddit.SubredditName == name);
                if (found == default(RedditChannelBinding)) return false;

                db.RedditChannelBindings.Remove(found);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            using (var db = new RedditContext(dbOptions))
            {
                var entities = db.RedditChannelBindings.Where(b => b.ChannelId == binding.Id);
                if (!entities.Any()) return false;

                db.RedditChannelBindings.RemoveRange(entities);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public IEnumerable<RedditChannelBinding> GetBindings(IMessageChannel channel)
        {
            using (var db = new RedditContext(dbOptions))
                return db.RedditChannelBindings.Where(b => b.ChannelId == channel.Id).Include(b => b.Subreddit).ToList();
        }

        async void IService.Init(dynamic config, IServiceProvider map)
        {
            dbOptions = map.GetService(typeof(DbContextOptions)) as DbContextOptions ?? throw new ServiceNotFoundException("Database Options were not found in service provider.");

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

        async Task ITimedService.Callback()
        {
            using (var db = new RedditContext(dbOptions))
            {
                foreach (var subreddit in db.RedditBindings.Include(b => b.ChannelBindings))
                {
                    var responseText = await httpClient.HttpGet($"{subreddit.SubredditName}/new/.json");
                    if (responseText == null) continue;

                    dynamic responseObject = JsonConvert.DeserializeObject(responseText);
                    dynamic posts = responseObject["data"]["children"];
                    var newestCreationTime = DateTimeOffset.FromUnixTimeSeconds((int)posts[0]["data"]["created_utc"]);
                    var postStack = new Stack<Embed>();

                    foreach (var post in posts)
                    {
                        var creationTime = DateTimeOffset.FromUnixTimeSeconds((int)post["data"]["created_utc"]);
                        if (creationTime <= subreddit.LatestPost) break;

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
                            .AddField(f => f.WithName("Subreddit").WithValue($"/r/{subreddit.SubredditName}").WithIsInline(true))
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
                                if (sauce.SimilarityPercentage > 90) postStack.Push(sauce.Embed);
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
                        foreach (var channelBinding in subreddit.ChannelBindings)
                        {
                            if (client.GetChannel(channelBinding.ChannelId) is IMessageChannel channel) await channel.SendEmbedAsync(embed);
                        }
                    }

                    if (newestCreationTime > subreddit.LatestPost) subreddit.LatestPost = newestCreationTime;

                    db.RedditBindings.Update(subreddit);
                }

                await db.SaveChangesAsync();
            }
        }
    }
}
