using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Xml;
using System.ServiceModel.Syndication;
using System.Timers;

namespace Shinoa.Net.Module
{
    class MALModule : IModule
    {
        class MALHistoryEntry
        {
            public string user;
            public string title;
            public string status;
            public string dateTime;

            public override bool Equals(object obj)
            {
                if (obj is MALHistoryEntry)
                {
                    var malHistoryObject = (MALHistoryEntry) obj;

                    return user.Equals(malHistoryObject.user) &&
                           title.Equals(malHistoryObject.title) &&
                           status.Equals(malHistoryObject.status) &&
                           dateTime.Equals(malHistoryObject.dateTime);
                }
                else
                {
                    return false;
                }
            }
        }

        class TrackedUser
        {
            public string username;
            public DateTimeOffset lastChecked = DateTimeOffset.Now;
        }

        List<TrackedUser> TrackedUsers = new List<TrackedUser>();
        Timer UpdateTimer = new Timer() { Interval = 1000 * 30 };

        public void Init()
        {
            foreach (var username in ShinoaNet.Config["myanimelist"])
            {
                TrackedUsers.Add(new TrackedUser() { username = username });
            }

            UpdateTimer.Elapsed += (s, e) =>
            {
                foreach (var user in TrackedUsers)
                {
                    Logging.Log($"Checking for new updates from MAL user {user.username}...");

                    using (XmlReader xmlReader = XmlReader.Create("http://myanimelist.net/rss.php?type=rwe&u=" + user.username))
                    {
                        var feed = SyndicationFeed.Load(xmlReader);

                        //var newItems = from item in feed.Items
                        //               where item.PublishDate > DateTimeOffset.Now.AddHours(12)
                        //               select item;

                        foreach (var item in feed.Items.Take(5))
                        {
                            Logging.Log($"{item.PublishDate} > {user.lastChecked} == {item.PublishDate > user.lastChecked}");
                        }

                        //foreach (var item in newItems)
                        //foreach (var item in feed.Items.Take(5))
                        //{
                        //    Logging.Log($"New MAL update: {item.Title.Text}");
                        //}

                        user.lastChecked = DateTimeOffset.Now;
                    }
                }
            };

            UpdateTimer.Start();
        }

        public string DetailedStats()
        {
            return null;
        }        

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            return;
        }
    }
}
