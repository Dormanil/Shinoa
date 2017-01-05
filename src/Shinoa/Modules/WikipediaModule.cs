using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class WikipediaModule : Abstract.HttpClientModule
    {
        public override void Init()
        {
            this.BaseUrl = "https://en.wikipedia.org/w/api.php";

            this.BoundCommands.Add("wiki", (c) =>
            {
                var queryText = GetCommandParametersAsString(c.Message.Content);
                var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;

                var responseText = HttpGet($"?action=opensearch&search={queryText}");
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                try
                {
                    string url = responseObject[3][0];
                    responseMessage.ModifyAsync(p => p.Content = url);
                }
                catch (ArgumentException)
                {
                    responseMessage.ModifyAsync(p => p.Content = "Search returned no results.");

                }
                catch (RuntimeBinderException)
                {
                    responseMessage.ModifyAsync(p => p.Content = "Search returned no results.");
                }
            });
        }
    }
}
