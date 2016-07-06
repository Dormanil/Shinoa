using System.Collections.Generic;
using Discord;
using RestSharp;
using RedditSharp.Things;
using System.Timers;
using Newtonsoft.Json;

namespace Shinoa.Net.Module
{
    class RedditModule : IModule
    {
        class SubscribedSubreddit
        {
            public string subreddit;
            public List<Channel> channels = new List<Channel>();            
            public Queue<string> lastPostIds = new Queue<string>();
        }

        List<SubscribedSubreddit> SubscribedSubreddits = new List<SubscribedSubreddit>();
        static RestClient RestClient = new RestClient("https://www.reddit.com");
        Timer UpdateTimer = new Timer { Interval = 1000 * 20 };

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            foreach (var subreddit in ShinoaNet.Config["reddit"])
            {
                var newSubscribedSubreddit = new SubscribedSubreddit();
                newSubscribedSubreddit.subreddit = subreddit["subreddit"];

                foreach (var channel in subreddit["channels"])
                {
                    newSubscribedSubreddit.channels.Add(ShinoaNet.DiscordClient.GetChannel(ulong.Parse(channel)));
                }

                SubscribedSubreddits.Add(newSubscribedSubreddit);

                Logging.Log($"> Loaded subreddit {newSubscribedSubreddit.subreddit} (bound to {newSubscribedSubreddit.channels.Count} channels).");
            }

            UpdateTimer.Elapsed += (s, e) =>
            {
                foreach (var subreddit in SubscribedSubreddits)
                {
                    var request = new RestRequest($"{subreddit.subreddit}/new/.json");

                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);
                    }

                    dynamic responseObject = JsonConvert.DeserializeObject(response.Content);

                    dynamic newestPost = responseObject["data"]["children"][0];

                    if (subreddit.lastPostIds.Count == 0)
                    {
                        subreddit.lastPostIds.Enqueue(newestPost["data"]["id"]);
                    }
                    else if (!subreddit.lastPostIds.Contains(newestPost["data"]["id"]))
                    {
                        subreddit.lastPostIds.Enqueue(newestPost["data"]["id"]);

                        var channelMessage = "";

                        channelMessage += $"**{newestPost["data"]["title"]}** `({newestPost["data"]["domain"]})`\n";
                        channelMessage += $"Posted to `{subreddit.subreddit}` by `/u/{newestPost["data"]["author"]}`\n";
                        channelMessage += $"<http://redd.it/{newestPost["data"]["id"]}>\n";

                        foreach(var channel in subreddit.channels)
                        {
                            channel.SendMessage(channelMessage);
                        }
                    }

                    while (subreddit.lastPostIds.Count > 5) subreddit.lastPostIds.Dequeue();
                }
            };

            UpdateTimer.Start();
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            return;
        }
    }
}
