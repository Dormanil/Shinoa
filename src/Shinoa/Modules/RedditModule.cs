using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Shinoa.Attributes;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class RedditModule : Abstract.UpdateLoopModule
    {
        Color MODULE_COLOR = new Color(255, 152, 0);
        HttpClient httpClient = new HttpClient();

        string[] CompactKeywords =
        {
            "spoiler"
        };

        string[] FilterKeywords =
        {

        };


        class RedditBinding
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }
            
            public string ChannelId { get; set; }
            public string SubredditName { get; set; }
        }

        class SubscribedSubreddit
        {
            public string subreddit;
            public List<ITextChannel> channels = new List<ITextChannel>();
            public Queue<string> lastPostIds = new Queue<string>();
        }

        List<SubscribedSubreddit> SubscribedSubreddits = new List<SubscribedSubreddit>();

        public override void Init()
        {
            Shinoa.DatabaseConnection.CreateTable<RedditBinding>();
            httpClient.BaseAddress = new Uri("https://www.reddit.com/");

            foreach (var boundSubreddit in Shinoa.DatabaseConnection.Table<RedditBinding>())
            {
                var channel = Shinoa.DiscordClient.GetChannel(ulong.Parse(boundSubreddit.ChannelId));

                if (channel is IPrivateChannel)
                {
                    var privateChannel = channel as IPrivateChannel;
                    var channelName = privateChannel.Name;

                    Logging.Log($"  /r/{boundSubreddit.SubredditName} -> [[PM] -> {channelName}]");
                }
                else if (channel is IGuildChannel)
                {
                    var guildChannel = channel as IGuildChannel;
                    var channelName = guildChannel.Name;
                    var guildName = guildChannel.Guild.Name;

                    Logging.Log($"  /r/{boundSubreddit.SubredditName} -> [{guildName} -> #{channelName}]");
                }
            }
        }

        [@Command("reddit")]
        public void RedditManagement(CommandContext c, params string[] args)
        {
            if (c.Channel is IPrivateChannel || (c.User as IGuildUser).GuildPermissions.ManageGuild)
            {
                var channelIdString = c.Channel.Id.ToString();

                if (args[0] == "add")
                {
                    var subredditName = args[1].Replace("r/", "").Replace("/", "").ToLower().Trim();

                    if (Shinoa.DatabaseConnection.Table<RedditBinding>()
                        .Where(item => item.ChannelId == channelIdString && item.SubredditName == subredditName).Count() == 0)
                    {
                        Shinoa.DatabaseConnection.Insert(new RedditBinding()
                        {
                            ChannelId = c.Channel.Id.ToString(),
                            SubredditName = subredditName
                        });

                        c.Channel.SendMessageAsync($"Notifications for /r/{subredditName} have been bound to this channel (#{c.Channel.Name}).");
                    }
                    else
                    {
                        c.Channel.SendMessageAsync($"Notifications for /r/{subredditName} are already bound to this channel (#{c.Channel.Name}).");
                    }
                }
                else if (args[0] == "remove")
                {
                    var subredditName = args[1].Replace("r/", "").Replace("/", "").ToLower().Trim();

                    if (Shinoa.DatabaseConnection.Table<RedditBinding>()
                        .Where(item => item.ChannelId == channelIdString && item.SubredditName == subredditName).Count() == 1)
                    {
                        var currentEntry = Shinoa.DatabaseConnection.Table<RedditBinding>()
                            .Where(item => item.ChannelId == channelIdString && item.SubredditName == subredditName).First();

                        Shinoa.DatabaseConnection.Delete(new RedditBinding() { Id = currentEntry.Id });

                        c.Channel.SendMessageAsync($"Notifications for /r/{subredditName} have been unbound from this channel (#{c.Channel.Name}).");
                    }
                    else
                    {
                        c.Channel.SendMessageAsync($"Notifications for /r/{subredditName} are not currently bound to this channel (#{c.Channel.Name}).");
                    }
                }
                else if (args[0] == "list")
                {
                    var response = "";
                    foreach (var binding in Shinoa.DatabaseConnection.Table<RedditBinding>()
                        .Where(item => item.ChannelId == channelIdString))
                    {
                        response += $"/r/{binding.SubredditName}\n";
                    }

                    if (response == "") response = "N/A";

                    var embed = new EmbedBuilder()
                        .AddField(f => f.WithName("Subreddits currently bound to this channel").WithValue(response))
                        .WithColor(MODULE_COLOR);

                    c.Channel.SendEmbedAsync(embed.Build());
                }
            }
            else
            {
                c.Channel.SendPermissionErrorAsync("Manage Server");
            }
        }

        public async override Task UpdateLoop()
        {
            foreach (var subreddit in SubscribedSubreddits) subreddit.channels.Clear();
            foreach (var boundSubreddit in Shinoa.DatabaseConnection.Table<RedditBinding>())
            {
                if (SubscribedSubreddits.Any(item => item.subreddit == boundSubreddit.SubredditName))
                {
                    SubscribedSubreddits.Find(item => item.subreddit == boundSubreddit.SubredditName).channels.Add(
                        Shinoa.DiscordClient.GetChannel(ulong.Parse(boundSubreddit.ChannelId)) as ITextChannel);
                }
                else
                {
                    var newSubscribedSubreddit = new SubscribedSubreddit();
                    newSubscribedSubreddit.subreddit = boundSubreddit.SubredditName;
                    newSubscribedSubreddit.channels.Add(Shinoa.DiscordClient.GetChannel(ulong.Parse(boundSubreddit.ChannelId)) as ITextChannel);
                    this.SubscribedSubreddits.Add(newSubscribedSubreddit);
                }
            }

            foreach (var subreddit in SubscribedSubreddits)
            {
                var responseText = httpClient.HttpGet($"r/{subreddit.subreddit}/new/.json");
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                dynamic newestPost = responseObject["data"]["children"][0];

                if (subreddit.lastPostIds.Count == 0)
                {
                    subreddit.lastPostIds.Enqueue((string)newestPost["data"]["id"]);
                }
                else if (!subreddit.lastPostIds.Contains((string)newestPost["data"]["id"]))
                {
                    subreddit.lastPostIds.Enqueue((string)newestPost["data"]["id"]);
                    
                    var title = System.Net.WebUtility.HtmlDecode((string)newestPost["data"]["title"]);
                    var domain = newestPost["data"]["domain"];
                    var username = newestPost["data"]["author"];
                    var id = newestPost["data"]["id"];
                    string url = newestPost["data"]["url"];
                    string selftext = newestPost["data"]["selftext"];

                    var filtered = false;
                    foreach (var filter in FilterKeywords)
                    {
                        if (title.ToLower().Contains(filter.ToLower()))
                        {
                            filtered = true;
                            break;
                        }
                    }

                    if (filtered) break;

                    var compact = false;
                    foreach (var filter in CompactKeywords)
                    {
                        if (title.ToLower().Contains(filter.ToLower()))
                        {
                            compact = true;
                            break;
                        }
                    }

                    string imageUrl = null;
                    try
                    {
                        imageUrl = newestPost["data"]["preview"]["images"][0]["source"]["url"];
                    }
                    catch (Exception) { }

                    string thumbnailUrl = null;
                    if (newestPost["data"]["thumbnail"] != "self") thumbnailUrl = newestPost["data"]["thumbnail"];

                    var embed = new EmbedBuilder()
                        .AddField(f => f.WithName("Title").WithValue(title))
                        .AddField(f => f.WithName("Submitted By").WithValue($"/u/{username}").WithIsInline(true))
                        .AddField(f => f.WithName("Subreddit").WithValue($"/r/{subreddit.subreddit}").WithIsInline(true))
                        .AddField(f => f.WithName("Shortlink").WithValue($"http://redd.it/{id}").WithIsInline(true))
                        .WithColor(MODULE_COLOR);
                    
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

                        if (selftext != "") embed.AddField(f => f.WithName("Text").WithValue(selftext));
                    }

                    foreach (var channel in subreddit.channels)
                    {
                        await channel.SendEmbedAsync(embed.Build());
                    }

                    if (!compact)
                    {
                        if (imageUrl != null)
                        {
                            var sauce = SauceModule.GetSauce(imageUrl);
                            if (sauce.SimilarityPercentage > 90)
                            {
                                foreach (var channel in subreddit.channels)
                                {
                                    await channel.SendEmbedAsync(sauce.GetEmbed());
                                }
                            }
                        }
                    }
                }

                while (subreddit.lastPostIds.Count > 5) subreddit.lastPostIds.Dequeue();
            }                
            
        }

        public override string DetailedStats
        {
            get
            {
                var response = "";
                foreach (var boundSubreddit in Shinoa.DatabaseConnection.Table<RedditBinding>())
                {
                    var channel = Shinoa.DiscordClient.GetChannel(ulong.Parse(boundSubreddit.ChannelId));

                    if (channel is IPrivateChannel)
                    {
                        var privateChannel = channel as IPrivateChannel;
                        var channelName = privateChannel.Name;

                        response += $"/r/{boundSubreddit.SubredditName} -> [[PM] -> {channelName}]\n";
                    }
                    else if (channel is IGuildChannel)
                    {
                        var guildChannel = channel as IGuildChannel;
                        var channelName = guildChannel.Name;
                        var guildName = guildChannel.Guild.Name;

                        response += $"/r/{boundSubreddit.SubredditName} -> [{guildName} -> #{channelName}]\n";
                    }
                }

                return response.Trim();
            }
        }
    }
}