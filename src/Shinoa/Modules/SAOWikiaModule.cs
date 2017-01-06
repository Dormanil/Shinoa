using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;
using System.Net.Http;

namespace Shinoa.Modules
{
    public class SAOWikiaModule : Abstract.Module
    {
        HttpClient httpClient = new HttpClient();

        public override void Init()
        {
            httpClient.BaseAddress = new Uri("http://swordartonline.wikia.com/api/v1/");

            this.BoundCommands.Add("saowiki", (c) =>
            {
                var queryText = GetCommandParametersAsString(c.Message.Content);
                var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;

                var httpResponseText = httpClient.HttpGet($"Search/List/?query={queryText}");

                dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

                try
                {
                    dynamic firstResult = responseObject["items"][0];

                    var resultMessage = "";
                    resultMessage += $"{firstResult["url"]}";

                    responseMessage.ModifyAsync(p => p.Content = resultMessage);
                }
                catch (ArgumentException)
                {
                    responseMessage.ModifyAsync(p => p.Content = "Search returned no results.");

                }
                catch (RuntimeBinderException)
                {
                    responseMessage.ModifyAsync(p => p.Content = "Search returned no results.");
                }
                catch (Exception ex)
                {
                    responseMessage.ModifyAsync(p => p.Content = "Error encountered, article not found.");
                    Logging.Log(ex.ToString());
                }
            });
        }
    }
}
