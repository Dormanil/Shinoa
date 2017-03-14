// <copyright file="SAOWikiaModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Discord.Commands;
    using Microsoft.CSharp.RuntimeBinder;
    using Newtonsoft.Json;

    public class SaoWikiaModule : ModuleBase<SocketCommandContext>
    {
        private HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://swordartonline.wikia.com/api/v1/") };

        [Command("sao")]
        [Alias("saowiki", "saowikia")]
        public async Task SaoWikiaSearch([Remainder]string queryText)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var httpResponseText = httpClient.HttpGet($"Search/List/?query={queryText}");
            dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

            var responseMessage = await responseMessageTask;

            try
            {
                dynamic firstResult = responseObject["items"][0];

                var resultMessage = string.Empty;
                resultMessage += $"{firstResult["url"]}";

                await responseMessage.ModifyAsync(p => p.Content = resultMessage);
            }
            catch (ArgumentException)
            {
                await responseMessage.ModifyAsync(p => p.Content = "Search returned no results.");
            }
            catch (RuntimeBinderException)
            {
                await responseMessage.ModifyAsync(p => p.Content = "Search returned no results.");
            }
            catch (Exception ex)
            {
               await responseMessage.ModifyAsync(p => p.Content = "Error encountered, article not found.");
               await Logging.Log(ex.ToString());
            }
        }
    }
}
