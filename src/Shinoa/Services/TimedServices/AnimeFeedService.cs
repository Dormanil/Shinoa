// <copyright file="AnimeFeedService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services.TimedServices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    using Databases;

    using Discord;
    using Discord.WebSocket;

    using static Databases.AnimeFeedContext;

    public class AnimeFeedService : IDatabaseService, ITimedService
    {
        private static readonly Color ModuleColor = new Color(0, 150, 136);
        private readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://www.nyaa.si/") };
        private AnimeFeedContext db;
        private DiscordSocketClient client;

        public bool AddBinding(IMessageChannel channel)
        {
            if (db.AnimeFeedBindings.Any(b => b.ChannelId == channel.Id)) return false;

            db.Add(new AnimeFeedBinding
            {
                ChannelId = channel.Id,
            });
            return true;
        }

        public bool RemoveBinding(IEntity<ulong> binding)
        {
            var entities = db.AnimeFeedBindings.Where(b => b.ChannelId == binding.Id);
            if (!entities.Any()) return false;

            db.AnimeFeedBindings.RemoveRange(entities);
            return true;
        }

        void IService.Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(AnimeFeedContext)) as AnimeFeedContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");

            client = map.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient ?? throw new ServiceNotFoundException("Database context was not found in service provider.");
        }

        Task IDatabaseService.Callback() => db.SaveChangesAsync();

        async Task ITimedService.Callback()
        {
            var responseText = await httpClient.HttpGet("?page=rss&u=HorribleSubs");
            if (responseText == null) return;

            var document = XDocument.Load(new MemoryStream(Encoding.Unicode.GetBytes(responseText)));
            var entries =
                document.Root.Descendants()
                    .First(i => i.Name.LocalName == "channel")
                    .Elements()
                    .Where(i => i.Name.LocalName == "item").ToList();

            var newestCreationTimeString = entries[0].Elements()
                .First(i => i.Name.LocalName == "pubDate").Value.Replace(" -0000", string.Empty);
            var newestCreationTime = DateTime.ParseExact(newestCreationTimeString, "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            var postStack = new Stack<Embed>();

            foreach (var entry in entries)
            {
                var creationTime = DateTime.ParseExact(entry.Elements().First(i => i.Name.LocalName.ToLower() == "pubdate").Value.Replace(" -0000", string.Empty), "ddd, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                if (creationTime <= AnimeFeedBinding.LatestPost) break;

                var title = entry.Elements().First(i => i.Name.LocalName == "title").Value;

                var match = Regex.Match(title, @"^\[.*\] (?<title>.*) - (?<ep>.*) \[.*$");
                if (!match.Success) continue;

                var showTitle = match.Groups["title"].Value;
                var episodeNumber = match.Groups["ep"].Value;

                var embed = new EmbedBuilder()
                    .AddField(f => f.WithName("New Episode").WithValue($"{showTitle} ep. {episodeNumber}"))
                    .WithColor(ModuleColor);

                postStack.Push(embed.Build());
            }

            if (newestCreationTime > AnimeFeedBinding.LatestPost) AnimeFeedBinding.LatestPost = newestCreationTime;

            foreach (var embed in postStack)
            {
                foreach (var channel in GetFromDb())
                {
                    await channel.SendEmbedAsync(embed);
                }
            }
        }

        private IEnumerable<IMessageChannel> GetFromDb()
        {
            return db.AnimeFeedBindings
                .Where(binding => client.GetChannel(binding.ChannelId) is ITextChannel)
                .Cast<IMessageChannel>();
        }
    }
}
