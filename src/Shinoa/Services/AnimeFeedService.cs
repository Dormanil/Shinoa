using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Shinoa.Attributes;
using SQLite;

namespace Shinoa.Services
{
    [Config("animefeed")]
    public class AnimeFeedService : IService
    {
        class AnimeFeedBinding
        {
            [PrimaryKey]
            public string ChannelId { get; set; }

            public DateTimeOffset LatestPost { get; set; }
        }

        private HttpClient httpClient = new HttpClient {BaseAddress = new Uri("http://www.nyaa.se/")};
        private SQLiteConnection db;
        private DiscordSocketClient client;
        private Timer refreshTimer;

        private Color moduleColor;

        void IService.Init(dynamic config, IDependencyMap map)
        {
            if(!map.TryGet(out db)) db = new SQLiteConnection(config["db_path"]);
            db.CreateTable<AnimeFeedBinding>();

            client = map.Get<DiscordSocketClient>();

            refreshTimer = new Timer(Callback, null, TimeSpan.Zero, TimeSpan.FromSeconds(int.Parse((string) config["refresh_rate"])));
        }

        void Callback(object state)
        {
            var responseText = httpClient.HttpGet("?page=rss&user=64513");
            var document = XDocument.Load(new MemoryStream(Encoding.Unicode.GetBytes(responseText)));
            var entries =
                document.Root.Descendants()
                    .First(i => i.Name.LocalName == "channel")
                    .Elements()
                    .Where(i => i.Name.LocalName == "item").ToList();

            var newestCreationTimeString = entries[0].Elements()
                .First(i => i.Name.LocalName == "pubDate").Value.Replace(" GMT", "").Replace(" +0000", "");
            var newestCreationTime = new DateTimeOffset(DateTime.ParseExact
                (newestCreationTimeString, "ddd, dd MMM yyyy H:m:s", CultureInfo.InvariantCulture));
            Stack<Embed> postStack = new Stack<Embed>();

            foreach (var entry in entries)
            {
                var creationTime = new DateTimeOffset(DateTime.ParseExact
                    (entry.Elements().First(i => i.Name.LocalName.ToLower() == "pubdate").Value, "ddd, dd MMM yyyy H:m:s zzz", CultureInfo.InvariantCulture));

                var title = entry.Elements().First(i => i.Name.LocalName == "title").Value;

                var match = Regex.Match(title, @"^\[.*\] (?<title>.*) - (?<ep>.*) \[.*$");
                if(!match.Success) continue;

                var showTitle = match.Groups["title"].Value;
                var episodeNumber = match.Groups["ep"].Value;

                var embed = new EmbedBuilder()
                    .AddField(f => f.WithName("New Episode").WithValue($"{showTitle} ep. {episodeNumber}"))
                    .WithColor(moduleColor);

                postStack.Push(embed.Build());
            }

            foreach (var embed in postStack)
            foreach (var channel in GetFromDb())
            {
                channel.SendEmbedAsync(embed).Wait();
            }
        }

        IEnumerable<IMessageChannel> GetFromDb()
        {
            List<IMessageChannel> ret = new List<IMessageChannel>();
            foreach (var binding in db.Table<AnimeFeedBinding>())
            {
                if(client.GetChannel(ulong.Parse(binding.ChannelId)) is ITextChannel channel) ret.Add(channel);
            }
            return ret;
        }

        public bool AddBinding(IMessageChannel channel)
        {
            if (db.Table<AnimeFeedBinding>().Any(b => b.ChannelId == channel.Id.ToString())) return false;

            db.Insert(new AnimeFeedBinding
            {
                ChannelId = channel.Id.ToString(),
                LatestPost = DateTimeOffset.UtcNow
            });
            return true;
        }

        public bool RemoveBinding(IMessageChannel channel)
        {
            return db.Delete<AnimeFeedBinding>(channel.Id.ToString()) != 0;
        }
    }
}
