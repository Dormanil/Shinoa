using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Tweetinvi;
using System.Timers;

namespace Shinoa.Net.Module
{
    class TwitterModule : IModule
    {
        class SubscribedUser
        {
            public string username;
            public List<Channel> channels = new List<Channel>();
            public Queue<long> lastTweetIds = new Queue<long>();
        }

        List<SubscribedUser> SubscribedUsers = new List<SubscribedUser>();
        Timer UpdateTimer = new Timer { Interval = 1000 * 20 };

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            var appCreds = Auth.SetApplicationOnlyCredentials(ShinoaNet.Config["twitter_key"], ShinoaNet.Config["twitter_secret"], true);
            Auth.InitializeApplicationOnlyCredentials(appCreds);

            foreach (var user in ShinoaNet.Config["twitter"])
            {
                var newSubscribedUser = new SubscribedUser();
                newSubscribedUser.username = user["user"];

                foreach (var channel in user["channels"])
                {
                    newSubscribedUser.channels.Add(ShinoaNet.DiscordClient.GetChannel(ulong.Parse(channel)));
                }

                SubscribedUsers.Add(newSubscribedUser);

                Logging.Log($"> Loaded user {newSubscribedUser.username} (bound to {newSubscribedUser.channels.Count} channels).");
            }

            UpdateTimer.Elapsed += (s, e) =>
            {
                foreach (var user in SubscribedUsers)
                {
                    var tweets = Timeline.GetUserTimeline(user.username);
                    var newestTweet = tweets.ToList()[0];

                    if (user.lastTweetIds.Count == 0)
                    {
                        user.lastTweetIds.Enqueue(newestTweet.Id);
                    }
                    else if (!user.lastTweetIds.Contains(newestTweet.Id))
                    {
                        user.lastTweetIds.Enqueue(newestTweet.Id);

                        var channelMessage = "";

                        channelMessage += $"New tweet from @{user.username} ({newestTweet.CreatedBy.Name}):\n\n";
                        channelMessage += $"```\n{newestTweet.FullText}\n```\n";
                        channelMessage += $"<{newestTweet.Url}>";
                        
                        foreach (var channel in user.channels)
                        {
                            channel.SendMessage(channelMessage);
                        }
                    }

                    while (user.lastTweetIds.Count > 5) user.lastTweetIds.Dequeue();
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
