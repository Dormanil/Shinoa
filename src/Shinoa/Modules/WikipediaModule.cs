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
                var responseText = HttpGet($"?action=opensearch&search={queryText}");
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                try
                {
                    string url = responseObject[3][0];
                    e.Channel.SendMessage(url);
                }
                catch (ArgumentException)
                {
                    e.Channel.SendMessage("Search returned no results.");

                }
                catch (RuntimeBinderException)
                {
                    e.Channel.SendMessage("Search returned no results.");
                }
            });
        }
    }
}
