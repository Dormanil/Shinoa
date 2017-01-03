using Discord;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class RedditModule : Abstract.HttpClientModule
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
            public List<Channel> channels = new List<Channel>();
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
                var channelName = channel.Name;
                var serverName = channel.IsPrivate ? "[PM]" : channel.Server.Name;

                Logging.Log($"  /r/{boundSubreddit.SubredditName} -> [{serverName} -> #{channelName}]");
            }

            this.BoundCommands.Add("reddit", (e) =>
            {
                if (e.Channel.IsPrivate || e.User.ServerPermissions.ManageServer)
                {
                    var channelIdString = e.Channel.Id.ToString();

                    if (GetCommandParameters(e.Message.Text)[0] == "add")
                    {
                        var subredditName = GetCommandParameters(e.Message.Text)[1].Replace("r/", "").Replace("/", "").ToLower().Trim();

                        if (Shinoa.DatabaseConnection.Table<RedditBinding>()
                            .Where(item => item.ChannelId == channelIdString && item.SubredditName == subredditName).Count() == 0)
                        {
                            Shinoa.DatabaseConnection.Insert(new RedditBinding()
                            {
                                ChannelId = e.Channel.Id.ToString(),
                                SubredditName = subredditName
                            });

                            e.Channel.SendMessage($"Notifications for /r/{subredditName} have been bound to this channel (#{e.Channel.Name}).");
                        }
                        else
                        {
                            e.Channel.SendMessage($"Notifications for /r/{subredditName} are already bound to this channel (#{e.Channel.Name}).");
                        }
                    }
                    else if (GetCommandParameters(e.Message.Text)[0] == "remove")
                    {
                        var subredditName = GetCommandParameters(e.Message.Text)[1].Replace("r/", "").Replace("/", "").ToLower().Trim();

                        if (Shinoa.DatabaseConnection.Table<RedditBinding>()
                            .Where(item => item.ChannelId == channelIdString && item.SubredditName == subredditName).Count() == 1)
                        {
                            var currentEntry = Shinoa.DatabaseConnection.Table<RedditBinding>()
                                .Where(item => item.ChannelId == channelIdString && item.SubredditName == subredditName).First();

                            Shinoa.DatabaseConnection.Delete(new RedditBinding() { Id = currentEntry.Id });

                            e.Channel.SendMessage($"Notifications for /r/{subredditName} have been unbound from this channel (#{e.Channel.Name}).");
                        }
                        else
                        {
                            e.Channel.SendMessage($"Notifications for /r/{subredditName} are not currently bound to this channel (#{e.Channel.Name}).");
                        }
                    }
                    else if (GetCommandParameters(e.Message.Text)[0] == "list")
                    {
                        var response = "Subreddits currently bound to this channel:\n";
                        foreach (var binding in  Shinoa.DatabaseConnection.Table<RedditBinding>()
                            .Where(item => item.ChannelId == channelIdString))
                        {
                            response += $"- {binding.SubredditName}\n";
                        }

                        e.Channel.SendMessage(response.Trim());
                    }
                }
                else
                {
                    e.Channel.SendPermissionError("Manage Server");
                }
            });

            this.UpdateLoop();
        }

        async Task UpdateLoop()
        {
            while (true)
            {
                foreach (var subreddit in SubscribedSubreddits) subreddit.channels.Clear();
                foreach (var boundSubreddit in Shinoa.DatabaseConnection.Table<RedditBinding>())
                {
                    if (SubscribedSubreddits.Any(item => item.subreddit == boundSubreddit.SubredditName))
                    {
                        SubscribedSubreddits.Find(item => item.subreddit == boundSubreddit.SubredditName).channels.Add(
                            Shinoa.DiscordClient.GetChannel(ulong.Parse(boundSubreddit.ChannelId)));
                    }
                    else
                    {
                        var newSubscribedSubreddit = new SubscribedSubreddit();
                        newSubscribedSubreddit.subreddit = boundSubreddit.SubredditName;
                        newSubscribedSubreddit.channels.Add(Shinoa.DiscordClient.GetChannel(ulong.Parse(boundSubreddit.ChannelId)));
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
                            await channel.SendMessage(channelMessage);
                        }
                    }

                    while (subreddit.lastPostIds.Count > 5) subreddit.lastPostIds.Dequeue();
                }

                await Task.Delay(UPDATE_INTERVAL);
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
                    var channelName = channel.Name;
                    var serverName = channel.IsPrivate ? "[PM]" : channel.Server.Name;

                    response += $"/r/{boundSubreddit.SubredditName} -> [{serverName} -> #{channelName}]\n";
                }

                return response.Trim();
            }
        }
    }
}