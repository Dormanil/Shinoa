using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using RedditSharp;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using System.Timers;
using RedditSharp.Things;

namespace Shinoa.Net.Module
{
    class RedditModule : IModule
    {
        class SubscribedSubreddit
        {
            public Subreddit subreddit;
            public List<Channel> channels = new List<Channel>();
        }

        List<SubscribedSubreddit> SubscribedSubreddits = new List<SubscribedSubreddit>();

        static RestClient RestClient = new RestClient("https://www.reddit.com/api/v1/");
        static Reddit Reddit;
        Timer UpdateTimer = new Timer { Interval = 1000 * 20 };

        Queue<Post> PostQueue = new Queue<Post>(10);

        public void Init()
        {
            RestClient.Authenticator = new RestSharp.Authenticators.HttpBasicAuthenticator(
                ShinoaNet.Config["reddit_client_id"], 
                ShinoaNet.Config["reddit_client_secret"]);

            var request = new RestRequest("access_token", Method.POST);
            request.AddParameter("grant_type", "password");
            request.AddParameter("username", ShinoaNet.Config["reddit_username"]);
            request.AddParameter("password", ShinoaNet.Config["reddit_password"]);

            IRestResponse response = RestClient.Execute(request);

            dynamic responseObject = JsonConvert.DeserializeObject(response.Content);
            string accessToken = responseObject.access_token;

            Logging.Log($"Reddit access token: {accessToken}");

            Reddit = new Reddit(accessToken: accessToken);

            foreach (var subreddit in ShinoaNet.Config["reddit"])
            {
                var newSubscribedSubreddit = new SubscribedSubreddit();
                newSubscribedSubreddit.subreddit = Reddit.GetSubreddit(subreddit["subreddit"]);

                foreach (var channel in subreddit["channels"])
                {
                    newSubscribedSubreddit.channels.Add(ShinoaNet.DiscordClient.GetChannel(ulong.Parse(channel)));
                }

                SubscribedSubreddits.Add(newSubscribedSubreddit);

                Logging.Log($"> Loaded subreddit /r/{newSubscribedSubreddit.subreddit.Name} (bound to {newSubscribedSubreddit.channels.Count} channels).");
            }

            //UpdateTimer.Elapsed += (s, e) =>
            //{
            //    foreach(var sub in SubscribedSubreddits)
            //    {
            //        foreach (var post in sub.subreddit.New.Take(1))
            //        {
            //            foreach (var channel in sub.channels)
            //            {
            //                channel.SendMessage(post.Title);
            //            }
            //        }
            //    }
            //};

            bool initialRun = true;
            UpdateTimer.Elapsed += (s, e) =>
            {
                foreach (var subreddit in SubscribedSubreddits)
                {
                    foreach (var post in subreddit.subreddit.New.Take(5))
                    {
                        bool alreadyProcessed = false;
                        foreach (var queueItem in PostQueue)
                        {
                            if (queueItem.Id.Equals(post.Id))
                            {
                                alreadyProcessed = true;
                                break;
                            }
                        }

                        if (!alreadyProcessed)
                        {
                            PostQueue.Enqueue(post);

                            if (!initialRun)
                            {
                                Logging.Log($"New post in subreddit '{subreddit.subreddit.Name}': {post.Title}");
                                foreach (var channel in subreddit.channels)
                                {
                                    channel.SendMessage($"**{post.Title}** ({post.Domain})\nPosted to /r/{post.SubredditName} by /u/{post.AuthorName}, {post.CommentCount} comments\n\n{post.Shortlink}");
                                }
                            }
                        }
                    }
                }

                if (initialRun) initialRun = false;
            };

            UpdateTimer.Start();
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
        }

        public string DetailedStats()
        {
            var output = "";

            foreach (var subreddit in SubscribedSubreddits)
            {
                output += $"/r/{subreddit.subreddit.Name} -> ";
                output += subreddit.channels.Count > 1 ? $"{subreddit.channels.Count} channels" : $"[{subreddit.channels[0].Server.Name} -> #{subreddit.channels[0].Name}]";
                output += '\n';
            }

            return output.Trim();
        }
    }
}
