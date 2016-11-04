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
using System.IO;

namespace Shinoa.Net.Module
{
    class AnimeNotificationsModule : IModule
    {
        static string FeedUrl = "http://www.nyaa.se/?page=rss&user=64513";

        public static List<Channel> BoundChannels = new List<Channel>();
        public static List<Channel> BoundUsers = new List<Channel>();
        public static Timer UpdateTimer = new Timer { Interval = 1000 * 30 };
                
        Queue<string> itemQueue = new Queue<string>(5);

        public void Init()
        {
            LoadConfig();

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

                        Regex regex = new Regex(@"^\[.*\] (?<title>.*) - (?<ep>.*) \[.*$");
                        Match match = regex.Match(entryTitle);
                        if (match.Success)
                        {
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
                                    foreach (var channel in BoundChannels)
                                    {
                                        channel.SendMessage($"```\nNew Episode: {showTitle} ep. {episodeNumber}\n```");
                                    }

                                    foreach (var privateChannel in BoundUsers)
                                    {
                                        privateChannel.SendMessage($"```\nNew Episode: {showTitle} ep. {episodeNumber}\n```");
                                    }
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
            if (e.Message.Channel.IsPrivate)
            {
                if (e.Message.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
                {
                    var cleanMessage = e.Message.RawText.Trim().ToLower();

                    if (cleanMessage == "!animenotifications on")
                    {
                        // NotificationUsers["anime_notification_users"].Add(e.Channel.Id);
                        BoundUsers.Add(e.Channel);
                        e.Channel.SendMessage("Anime notifications enabled.");
                    }
                    else if (cleanMessage == "!animenotifications off")
                    {
                        // NotificationUsers["anime_notification_users"].Remove(e.Channel.Id);
                        BoundUsers.Remove(e.Channel);
                        e.Channel.SendMessage("Anime notifications disabled.");
                    }

                    WriteConfig();
                }
            }
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

        void WriteConfig()
        {
            using (var streamWriter = new StreamWriter("anime_notification_users.yaml", false))
            {
                dynamic notificationUsers = new
                {
                    anime_notification_users = new List<ulong>()
                };

                foreach(var privateChannel in BoundUsers)
                {
                    notificationUsers.anime_notification_users.Add(privateChannel.Id);
                }

                var serializer = new YamlDotNet.Serialization.Serializer();
                serializer.Serialize(streamWriter, notificationUsers);
            }
        }

        void LoadConfig()
        {
            using (var streamReader = new StreamReader("anime_notification_users.yaml"))
            {
                dynamic notificationUsers;

                var deserializer = new YamlDotNet.Serialization.Deserializer();
                notificationUsers = deserializer.Deserialize(streamReader);

                foreach (string channelId in notificationUsers["anime_notification_users"])
                {
                    var channel = ShinoaNet.DiscordClient.GetChannel(ulong.Parse(channelId));
                    BoundUsers.Add(channel);
                    Logging.Log($"> Bound anime notifications to [[PM] -> #{channel.Name}]");
                }
            }            
        }
    }
}
