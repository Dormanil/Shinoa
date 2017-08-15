// <copyright file="WikipediaModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Attributes;

    using Discord;
    using Discord.Commands;

    using Extensions;

    using Microsoft.CSharp.RuntimeBinder;

    using Newtonsoft.Json;

    /// <summary>
    /// Module to search Wikipedia.
    /// </summary>
    [RequireNotBlacklisted]
    public class WikipediaModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Color ModuleColor = new Color(33, 150, 243);
        private static readonly HttpClient HttpClient = new HttpClient { BaseAddress = new Uri("https://en.wikipedia.org/w/") };

        /// <summary>
        /// Searches Wikipedia.
        /// </summary>
        /// <param name="queryText">Query to search for.</param>
        /// <returns></returns>
        [Command("wiki")]
        [Alias("wikipedia", "wikisearch")]
        public async Task WikipediaSearch([Remainder]string queryText)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var responseText = await HttpClient.HttpGet($"api.php?action=opensearch&search={queryText}");
            if (responseText == null)
            {
                var responseMsg = await responseMessageTask;
                await responseMsg.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle("Search returned no results.")
                    .WithColor(ModuleColor)
                    .Build());
                return;
            }

            dynamic responseObject = JsonConvert.DeserializeObject(responseText);

            var responseMessage = await responseMessageTask;

            try
            {
                string title = responseObject[1][0];
                string url = responseObject[3][0];
                string firstParagraph = responseObject[2][0];

                var embed = new EmbedBuilder()
                    .WithTitle(title)
                    .WithUrl(url)
                    .WithDescription(firstParagraph)
                    .WithColor(ModuleColor);

                await responseMessage.ModifyToEmbedAsync(embed.Build());
            }
            catch (ArgumentException)
            {
                await responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle($"Search returned no results.")
                    .WithColor(ModuleColor)
                    .Build());
            }
            catch (RuntimeBinderException)
            {
                await responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle($"Search returned no results.")
                    .WithColor(ModuleColor)
                    .Build());
            }
        }
    }
}
