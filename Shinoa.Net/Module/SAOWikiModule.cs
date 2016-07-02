using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;
using RestSharp;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace Shinoa.Net.Module
{
    class SAOWikiModule : IModule
    {
        static RestClient RestClient = new RestClient("http://swordartonline.wikia.com/api/v1/");

        public void Init()
        {
            // do nothing
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                var regex = new Regex(@"^!saowiki (?<querytext>.*)");
                if (regex.IsMatch(e.Message.Text))
                {
                    var queryText = regex.Matches(e.Message.Text)[0].Groups["querytext"];

                    Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] {e.User.Name} searched SAO Wikia for '{queryText}'.");

                    var request = new RestRequest($"Search/List/?query={queryText}");

                    // Retry until the request successfully goes through.

                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);
                    }

                    dynamic responseObject = JsonConvert.DeserializeObject(response.Content);

                    try
                    {
                        dynamic firstResult = responseObject["items"][0];

                        var responseMessage = "";
                        responseMessage += $"{firstResult["url"]}";

                        e.Channel.SendMessage(responseMessage);
                    }
                    catch (ArgumentException ex)
                    {
                        Logging.Log("SAO Wikia Module request encountered an ArgumentException.");
                        Logging.Log($"ResponseObject is: {responseObject}");

                    }
                    catch (RuntimeBinderException ex)
                    {
                        e.Channel.SendMessage("Search returned no results.");
                    }
                    catch (Exception ex)
                    {
                        e.Channel.SendMessage("Error encountered, article not found.");
                        Logging.Log(ex.ToString());
                    }
                }
            }
        }
        
        public string DetailedStats()
        {
            return null;
        }
    }
}
