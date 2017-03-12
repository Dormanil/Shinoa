using Discord;
using Discord.Commands;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Shinoa.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class WikipediaModule : Abstract.Module
    {
        public static Color MODULE_COLOR = new Color(33, 150, 243);
        HttpClient httpClient = new HttpClient();
        
        [@Command("wiki", "wikipedia", "wikisearch")]
        public async Task WikipediaSearch(CommandContext c, params string[] args)
        {
            var queryText = GetCommandParametersAsString(c.Message.Content);
            var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;

            var responseText = httpClient.HttpGet($"https://en.wikipedia.org/w/api.php?action=opensearch&search={queryText}");
            dynamic responseObject = JsonConvert.DeserializeObject(responseText);

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
