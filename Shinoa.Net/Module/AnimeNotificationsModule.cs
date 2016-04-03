using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Timers;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;

namespace Shinoa.Net.Module
{
    class AnimeNotificationsModule : IModule
    {
        static string FeedUrl = "http://www.nyaa.se/?page=rss&user=64513";

        public static List<Channel> BoundChannels = new List<Channel>();
        public static Timer UpdateTimer = new Timer { Interval = 1000 * 30 };

        Queue<string> itemQueue = new Queue<string>(5);

        public void Init()
        {
            foreach (var channelId in ShinoaNet.Config["anime_notification_channels"])
            {
                var channel = ShinoaNet.DiscordClient.GetChannel(ulong.Parse(channelId));
                BoundChannels.Add(channel);

                Logging.Log($"> Bound anime notifications to [{channel.Server.Name} -> #{channel.Name}]");
            }

            bool initialRun = true;
            UpdateTimer.Elapsed += (s, e) =>
            {
                if (BoundChannels.Count > 0)
                {
                    var reader = XmlReader.Create(FeedUrl);
                    var feed = SyndicationFeed.Load(reader);
                    reader.Close();

                    foreach (var item in feed.Items.Take(5))
                    {
                        var entryTitle = item.Title.Text;

                        Regex regex = new Regex(@"^\[.*\] (?<title>.*) - (?<ep>\d+) \[.*$");
                        Match match = regex.Match(entryTitle);
                        var showTitle = match.Groups["title"].Value;
                        var episodeNumber = match.Groups["ep"].Value;

                        bool alreadyProcessed = false;
                        foreach (var queueItem in itemQueue)
                        {
                            if (queueItem.Equals(showTitle))
                            {
                                alreadyProcessed = true;
                                break;
                            }
                        }

                        if (!alreadyProcessed)
                        {                             
                            itemQueue.Enqueue(showTitle);

                            if (!initialRun)
                            {
                                //Logging.Log($"Found new episode: {showTitle} ep. {episodeNumber}");

                                foreach (var channel in BoundChannels)
                                {
                                    channel.SendMessage($"New Episode: `{showTitle}` ep. {episodeNumber}");
                                }
                            }
                        }
                    }

                    if (initialRun) initialRun = false;
                }
            };

            UpdateTimer.Start();
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
        }

        public string DetailedStats()
        {
            var output = "";

            foreach (var channel in BoundChannels)
            {
                output += $"-> [{channel.Server.Name} -> #{channel.Name}]";
            }

            return output.Trim();
        }
    }
}
