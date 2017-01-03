using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Shinoa.Modules
{
    public class AnimeFeedModule : Abstract.HttpClientModule
    {
        int UPDATE_INTERVAL = 20 * 1000;

        class AnimeFeedBinding
        {
            [PrimaryKey]
            public string ChannelId { get; set; }
        }

        string FEED_URL = "http://www.nyaa.se/?page=rss&user=64513";
        Queue<string> itemQueue = new Queue<string>(5);

        public override void Init()
        {
            Shinoa.DatabaseConnection.CreateTable<AnimeFeedBinding>();

            foreach (var binding in Shinoa.DatabaseConnection.Table<AnimeFeedBinding>())
            {
                var channel = Shinoa.DiscordClient.GetChannel(ulong.Parse(binding.ChannelId));
                var channelName = channel.IsPrivate ? channel.Name : "#" + channel.Name;
                var serverName = channel.IsPrivate ? "[PM]" : channel.Server.Name;

                Logging.Log($"  -> [{serverName} -> {channelName}]");
            }

            this.BoundCommands.Add("animefeed", (e) =>
            {
                if (e.Channel.IsPrivate || e.User.ServerPermissions.ManageServer)
                {
                    var channelIdString = e.Channel.Id.ToString();

                    if (GetCommandParameters(e.Message.Text)[0] == "enable")
                    {

                        if (Shinoa.DatabaseConnection.Table<AnimeFeedBinding>()
                            .Where(item => item.ChannelId == channelIdString).Count() == 0)
                        {
                            Shinoa.DatabaseConnection.Insert(new AnimeFeedBinding()
                            {
                                ChannelId = e.Channel.Id.ToString()
                            });

                            if (!e.Channel.IsPrivate)
                                e.Channel.SendMessage($"Anime notifications have been bound to this channel (#{e.Channel.Name}).");
                            else
                                e.Channel.SendMessage($"You will now receive anime notifications via PM.");
                        }
                        else
                        {
                            if (!e.Channel.IsPrivate)
                                e.Channel.SendMessage($"Anime notifications are already bound to this channel (#{e.Channel.Name}).");
                            else
                                e.Channel.SendMessage($"You are already receiving anime notifications.");
                        }
                    }
                    else if (GetCommandParameters(e.Message.Text)[0] == "disable")
                    {
                        if (Shinoa.DatabaseConnection.Table<AnimeFeedBinding>()
                            .Where(item => item.ChannelId == channelIdString).Count() == 1)
                        {
                            var currentEntry = Shinoa.DatabaseConnection.Table<AnimeFeedBinding>()
                                .Where(item => item.ChannelId == channelIdString).First();

                            Shinoa.DatabaseConnection.Delete(new AnimeFeedBinding() { ChannelId = channelIdString });

                            if (!e.Channel.IsPrivate)
                                e.Channel.SendMessage($"Anime notifications have been unbound from this channel (#{e.Channel.Name}).");
                            else
                                e.Channel.SendMessage($"You will no lonnger receive anime notifications.");
                        }
                        else
                        {
                            if (!e.Channel.IsPrivate)
                                e.Channel.SendMessage($"Anime notifications are not currently bound to this channel (#{e.Channel.Name}).");
                            else
                                e.Channel.SendMessage($"You are not currently receiving anime notifications.");
                        }
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
            bool initialRun = true;
            while (true)
            {
                var responseText = HttpGet(FEED_URL);
                var document = XDocument.Load(new MemoryStream(Encoding.Unicode.GetBytes(responseText)));
                var entries = document.Root.Descendants().First(i => i.Name.LocalName == "channel").Elements().Where(i => i.Name.LocalName == "item");

                foreach (var item in entries.Take(5))
                {
                    var entryTitle = item.Elements().First(i => i.Name.LocalName == "title").Value;

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
                                foreach (var binding in Shinoa.DatabaseConnection.Table<AnimeFeedBinding>())
                                {
                                    var channel = Shinoa.DiscordClient.GetChannel(ulong.Parse(binding.ChannelId));
                                    await channel.SendMessage($"```\nNew Episode: {showTitle} ep. {episodeNumber}\n```");
                                }
                            }
                        }
                    }
                }

                if (initialRun) initialRun = false;
                await Task.Delay(UPDATE_INTERVAL);           
            }
        }
    }
}
