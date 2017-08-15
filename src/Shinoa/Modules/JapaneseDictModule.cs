// <copyright file="JapaneseDictModule.cs" company="The Shinoa Development Team">
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
    using Discord.Commands;

    using Extensions;

    using Newtonsoft.Json;

    /// <summary>
    /// Module for access to Jisho.
    /// </summary>
    [RequireNotBlacklisted]
    public class JapaneseDictModule : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient HttpClient = new HttpClient { BaseAddress = new Uri("http://jisho.org/api/v1/search/") };

        /// <summary>
        /// Command to search Jisho.org for a term and return the response.
        /// </summary>
        /// <param name="term">A search term.</param>
        /// <returns></returns>
        [Command("jp")]
        [Alias("jisho", "jpdict", "japanese")]
        public async Task JishoSearch([Remainder] string term)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var httpResponseText = await HttpClient.HttpGet($"words?keyword={term}");
            if (httpResponseText == null)
            {
                var responseMsg = await responseMessageTask;
                await responseMsg.ModifyAsync(p => p.Content = "Not found.");
                return;
            }

            dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

            var responseMessage = await responseMessageTask;

            try
            {
                dynamic firstResult = responseObject["data"][0];

                var responseText = string.Empty;

                foreach (var word in firstResult["japanese"])
                {
                    var wordKanji = word["word"];
                    var wordReading = word["reading"];

                    if (wordKanji != null && wordReading != null) responseText += $"**{wordKanji}** - {wordReading}, ";
                    else if (wordKanji != null) responseText += $"**{wordKanji}**, ";
                    else if (wordReading != null) responseText += $"**{wordReading}**, ";
                }

                responseText = responseText.Trim(',', ' ');
                responseText += '\n';

                foreach (var sense in firstResult["senses"])
                {
                    responseText += "\u2022 ";

                    foreach (var definition in sense["english_definitions"])
                    {
                        responseText += $"{definition}, ";
                    }

                    responseText = responseText.Trim(',', ' ');

                    if (sense["parts_of_speech"].Count > 0)
                    {
                        responseText += " (";

                        foreach (string part in sense["parts_of_speech"])
                        {
                            responseText += $"{part.ToLower()}, ";
                        }

                        responseText = responseText.Trim(',', ' ');

                        responseText += ")";
                    }

                    responseText += '\n';
                }

                responseText += $"\nSee more: <http://jisho.org/search/{Uri.EscapeUriString(term)}>";

                await responseMessage.ModifyAsync(p => p.Content = responseText);
            }
            catch (Exception)
            {
                await responseMessage.ModifyAsync(p => p.Content = "Not found.");
            }
        }
    }
}
