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

            this.BoundCommands.Add("wiki", (e) =>
            {
                var queryText = GetCommandParametersAsString(e.Message.Text);
                var responseMessage = e.Channel.SendMessage("Searching...").Result;

                var responseText = HttpGet($"?action=opensearch&search={queryText}");
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                try
                {
                    string url = responseObject[3][0];
                    responseMessage.Edit(url);
                }
                catch (ArgumentException)
                {
                    responseMessage.Edit("Search returned no results.");

                }
                catch (RuntimeBinderException)
                {
                    responseMessage.Edit("Search returned no results.");
                }
            });
        }
    }
}
