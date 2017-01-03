using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;

namespace Shinoa.Modules
{
    public class SAOWikiaModule : Abstract.HttpClientModule
    {
        public override void Init()
        {
            this.BaseUrl = "http://swordartonline.wikia.com/api/v1/";

            this.BoundCommands.Add("saowiki", (e) =>
            {
                var queryText = GetCommandParametersAsString(e.Message.Text);
                var httpResponseText = HttpGet($"Search/List/?query={queryText}");

                dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

                try
                {
                    dynamic firstResult = responseObject["items"][0];

                    var responseMessage = "";
                    responseMessage += $"{firstResult["url"]}";

                    e.Channel.SendMessage(responseMessage);
                }
                catch (ArgumentException)
                {
                    e.Channel.SendMessage("Search returned no results.");

                }
                catch (RuntimeBinderException)
                {
                    e.Channel.SendMessage("Search returned no results.");
                }
                catch (Exception ex)
                {
                    e.Channel.SendMessage("Error encountered, article not found.");
                    Logging.Log(ex.ToString());
                }
            });
        }
    }
}
