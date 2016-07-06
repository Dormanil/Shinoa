using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using RestSharp;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;

namespace Shinoa.Net.Module
{
    class WikipediaModule : IModule
    {
        static RestClient RestClient = new RestClient("https://en.wikipedia.org/w/api.php");

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            // do nothing
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"wiki (?<querytext>.*)");
                if (regex.IsMatch(e.Message.Text))
                {
                    var queryText = regex.Matches(e.Message.Text)[0].Groups["querytext"];

                    Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] {e.User.Name} searched Wikipedia for '{queryText}'.");

                    var request = new RestRequest($"?action=opensearch&search={queryText}");

                    // Retry until the request successfully goes through.

                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);
                    }

                    dynamic responseObject = JsonConvert.DeserializeObject(response.Content);

                    try
                    {
                        string url = responseObject[3][0];

                        e.Channel.SendMessage(url);
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
    }
}
