using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Timers;
using System.Xml;
using System.Text.RegularExpressions;
using System.ServiceModel.Syndication;

namespace Shinoa.Net.Module
{
    class FeedModule : IModule
    {
        public static List<Feed> activeFeeds = new List<Feed>();
        static Timer UpdateTimer = new Timer { Interval = 1000 * 60 };
        Queue<string> ItemQueue = new Queue<string>(5);

        public class Feed
        {
            public string name;
            public string feedUrl;
            public List<Channel> boundChannels = new List<Channel>();
            
            override public string ToString()
            {
                var output = "";
                output += $"Name: {name}\nFeed URL: {feedUrl}\nBound to channels:\n";
                
                foreach (var channel in boundChannels)
                {
                    output += $"[{channel.Server.Name} -> #{channel.Name}]";
                }

                return output;
            }
        }

        public void Init()
        {
            foreach(var feed in ShinoaNet.Config["feeds"])
            {
                var newFeed = new Feed();
                newFeed.name = feed["name"];
                newFeed.feedUrl = feed["feed_url"];

                foreach (var channel in feed["channels"])
                {
                    newFeed.boundChannels.Add(ShinoaNet.DiscordClient.GetChannel(ulong.Parse(channel)));
                }

                Logging.Log(newFeed.ToString());

                activeFeeds.Add(newFeed);
            }

            bool initialRun = true;
            UpdateTimer.Elapsed += (s, e) =>
            {
                foreach (var currentFeed in activeFeeds)
                {
                    var reader = XmlReader.Create(currentFeed.feedUrl);
                    var feed = SyndicationFeed.Load(reader);
                    reader.Close();

                    foreach (var item in feed.Items.Take(5))
                    {
                        var entryTitle = item.Title.Text;

                        bool alreadyProcessed = false;
                        foreach (var queueItem in ItemQueue)
                        {
                            if (queueItem.Equals(entryTitle))
                            {
                                alreadyProcessed = true;
                                break;
                            }
                        }

                        if (!alreadyProcessed)
                        {
                            Logging.Log($"New feed item in feed '{currentFeed.name}': {entryTitle}");
                            ItemQueue.Enqueue(entryTitle);

                            if (!initialRun)
                            {
                                foreach (var channel in currentFeed.boundChannels)
                                {
                                    channel.SendMessage($"** -- New feed item -- **\n{entryTitle}\n{item.Links[0].GetAbsoluteUri().ToString()}");
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
    }
}
