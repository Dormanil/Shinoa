using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;

namespace Shinoa.Net
{
    class FeedNotifier
    {
        public class FeedEventArgs : EventArgs
        {
            public List<SyndicationItem> NewItems;
        }

        string FeedUrl;
        int CheckInterval;
        public event EventHandler<FeedEventArgs> NewItemsFound;

        Timer UpdateTimer;

        List<string> lastItemCollection;

        public FeedNotifier (string feedUrl, int checkIntervalSeconds)
        {
            this.FeedUrl = feedUrl;
            this.CheckInterval = checkIntervalSeconds;

            this.UpdateTimer = new Timer() { Interval = 1000 * checkIntervalSeconds };
            this.UpdateTimer.Start();

            this.UpdateTimer.Elapsed += (s, e) =>
            {
                using (XmlReader xmlReader = XmlReader.Create(feedUrl))
                {
                    var feed = SyndicationFeed.Load(xmlReader);

                    List<SyndicationItem> newItems = null;

                    if (lastItemCollection != null)
                    {
                        //foreach (var item in feed.Items)
                        //{
                        //    //foreach (var url in lastUrlCollection)
                        //    //{
                        //    //    if (url.Equals(item.Links[0].GetAbsoluteUri().ToString()))
                        //    //    {
                        //    //        newItems.Add(item);
                        //    //        break;
                        //    //    }
                        //    //}

                        //    //if (!lastUrlCollection.Contains(item.Links[0].GetAbsoluteUri().ToString()))
                        //    if (true)
                        //    {
                        //        newItems.Add(item);
                        //    }
                        //}

                        newItems =  (from currentItem in feed.Items
                                     where !lastItemCollection.Contains(currentItem.Title.Text)
                                     select currentItem).ToList();
                    }

                    lastItemCollection = new List<string>();
                    foreach (var item in feed.Items)
                    {
                        lastItemCollection.Add(item.Title.Text);
                    }

                    //lastItemCollection = feed.Items.ToList();

                    if (newItems != null && newItems.Count > 0)
                    {
                        var eventArgs = new FeedEventArgs();
                        eventArgs.NewItems = newItems;

                        Console.Write($"Found {newItems.Count} new items.");
                        NewItemsFound.Invoke(this, eventArgs);
                    }
                }
            };
        }
    }
}
