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
                var responseMessage = e.Channel.SendMessage("Searching...").Result;

                var httpResponseText = HttpGet($"Search/List/?query={queryText}");

                dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

                try
                {
                    dynamic firstResult = responseObject["items"][0];

                    var resultMessage = "";
                    resultMessage += $"{firstResult["url"]}";

                    responseMessage.Edit(resultMessage);
                }
                catch (ArgumentException)
                {
                    responseMessage.Edit("Search returned no results.");

                }
                catch (RuntimeBinderException)
                {
                    responseMessage.Edit("Search returned no results.");
                }
                catch (Exception ex)
                {
                    responseMessage.Edit("Error encountered, article not found.");
                    Logging.Log(ex.ToString());
                }
            });
        }
    }
}
