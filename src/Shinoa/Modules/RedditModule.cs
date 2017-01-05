using Discord;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class RedditModule : Abstract.HttpClientModule, Abstract.IUpdateLoop
    {
        int UPDATE_INTERVAL = 20 * 1000;

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
            this.BaseUrl = "https://www.reddit.com/";

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

            this.BoundCommands.Add("reddit", (c) =>
            {
                if (c.Channel is IPrivateChannel || (c.User as IGuildUser).GuildPermissions.ManageGuild)
                {
                    var channelIdString = c.Channel.Id.ToString();

                    if (GetCommandParameters(c.Message.Content)[0] == "add")
                    {
                        var subredditName = GetCommandParameters(c.Message.Content)[1].Replace("r/", "").Replace("/", "").ToLower().Trim();

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
                    else if (GetCommandParameters(c.Message.Content)[0] == "remove")
                    {
                        var subredditName = GetCommandParameters(c.Message.Content)[1].Replace("r/", "").Replace("/", "").ToLower().Trim();

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
                    else if (GetCommandParameters(c.Message.Content)[0] == "list")
                    {
                        var response = "Subreddits currently bound to this channel:\n";
                        foreach (var binding in  Shinoa.DatabaseConnection.Table<RedditBinding>()
                            .Where(item => item.ChannelId == channelIdString))
                        {
                            response += $"- /r/{binding.SubredditName}\n";
                        }

                        c.Channel.SendMessageAsync(response.Trim());
                    }
                }
                else
                {
                    c.Channel.SendPermissionErrorAsync("Manage Server");
                }
            });
        }

        public async Task UpdateLoop()
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
                var responseText = HttpGet($"r/{subreddit.subreddit}/new/.json");
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                dynamic newestPost = responseObject["data"]["children"][0];

                if (subreddit.lastPostIds.Count == 0)
                {
                    subreddit.lastPostIds.Enqueue((string)newestPost["data"]["id"]);
                }
                else if (!subreddit.lastPostIds.Contains((string)newestPost["data"]["id"]))
                {
                    subreddit.lastPostIds.Enqueue((string)newestPost["data"]["id"]);

                    var channelMessage = "";

                    channelMessage += $"**{System.Net.WebUtility.HtmlDecode((string)newestPost["data"]["title"])}** `({newestPost["data"]["domain"]})`\n";
                    channelMessage += $"Posted to `/r/{subreddit.subreddit}` by `/u/{newestPost["data"]["author"]}`\n";
                    channelMessage += $"<http://redd.it/{newestPost["data"]["id"]}>\n";

                    foreach (var channel in subreddit.channels)
                    {
                        await channel.SendMessageAsync(channelMessage);
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