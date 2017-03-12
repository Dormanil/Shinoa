using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;
using System.Net.Http;
using Discord.Commands;
using Shinoa.Attributes;

namespace Shinoa.Modules
{
    public class SAOWikiaModule : Abstract.Module
    {
        HttpClient httpClient = new HttpClient();

        public override void Init()
        {
            httpClient.BaseAddress = new Uri("http://swordartonline.wikia.com/api/v1/");
        }

        [@Command("sao", "saowiki", "saowikia")]
        public async Task SAOWikiaSearch(CommandContext c, params string[] args)
        {
            var queryText = args.ToRemainderString();
            var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;

            var httpResponseText = httpClient.HttpGet($"Search/List/?query={queryText}");

            dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

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
                await Logging.Log(ex.ToString());
            }
        }
    }
}
