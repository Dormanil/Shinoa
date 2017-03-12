using Discord;
using Discord.Commands;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class WikipediaModule : ModuleBase<SocketCommandContext>
    {
        public static Color MODULE_COLOR = new Color(33, 150, 243);
        HttpClient httpClient = new HttpClient();
        
        [Command("wiki"), Alias("wikipedia", "wikisearch")]
        public async Task WikipediaSearch([Remainder]string queryText)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var responseText = httpClient.HttpGet($"https://en.wikipedia.org/w/api.php?action=opensearch&search={queryText}");
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
                    .WithColor(MODULE_COLOR);

                await responseMessage.ModifyToEmbedAsync(embed.Build());
            }
            catch (ArgumentException)
            {
                await responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle($"Search returned no results.")
                    .WithColor(MODULE_COLOR)
                    .Build());

            }
            catch (RuntimeBinderException)
            {
                await responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle($"Search returned no results.")
                    .WithColor(MODULE_COLOR)
                    .Build());
            }
        }
    }
}
