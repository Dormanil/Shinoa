using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;
using System.Net.Http;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class SAOWikiaModule : ModuleBase<SocketCommandContext>
    {
        HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://swordartonline.wikia.com/api/v1/") };

        [Command("sao"), Alias("saowiki", "saowikia")]
        public async Task SAOWikiaSearch([Remainder]string queryText)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var httpResponseText = httpClient.HttpGet($"Search/List/?query={queryText}");
            dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

            var responseMessage = await responseMessageTask;

            try
            {
                dynamic firstResult = responseObject["items"][0];

                var resultMessage = "";
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
               Logging.Log(ex.ToString());
            }
        }
    }
}
