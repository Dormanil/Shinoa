using Discord;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
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

        public override void Init()
        {

            this.BoundCommands.Add("wiki", (c) =>
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
                    
                    responseMessage.ModifyToEmbedAsync(embed.Build());
                }
                catch (ArgumentException)
                {
                    responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                        .WithTitle($"Search returned no results.")
                        .WithColor(MODULE_COLOR)
                        .Build());

                }
                catch (RuntimeBinderException)
                {
                    responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                        .WithTitle($"Search returned no results.")
                        .WithColor(MODULE_COLOR)
                        .Build());
                }
            });
        }
    }
}
