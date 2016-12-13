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
    class GlosbeModule : IModule
    {
        static RestClient RestClient = new RestClient("https://glosbe.com/gapi/");

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"de (?<querytext>.*)");
                if (regex.IsMatch(e.Message.Text))
                {
                    var queryText = regex.Matches(e.Message.Text)[0].Groups["querytext"];
                    var request = new RestRequest($"translate?format=json&from=deu&dest=eng&phrase={queryText}");
                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);
                    }

                    dynamic responseObject = JsonConvert.DeserializeObject(response.Content);

                    Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] {e.User.Name} searched Glosbe for '{queryText}'.");

                    try
                    {
                        dynamic firstResult = responseObject["tuc"][0];

                        var responseText = "";

                        if (firstResult["phrase"]["language"] == "en") responseText += (firstResult["phrase"]["text"] != null ? $"**{firstResult["phrase"]["text"]}**" : "");
                        if (responseText != string.Empty)
                        {
                            responseText += '\n';
                            foreach (var meaning in firstResult["meanings"])
                            {
                                if(meaning["language"] == "en")
                                {
                                    responseText += "\u2022";
                                    responseText += meaning["text"];
                                    responseText += '\n';
                                }
                            }

                            responseText += $"\nSee more: <https://glosbe.com/de/en/{System.Uri.EscapeUriString(queryText.Value)}>";
                            e.Channel.SendMessage(responseText);
                        } else
                        {
                            e.Channel.SendMessage("Not found.");
                        }
                    } catch
                    {
                        e.Channel.SendMessage("Not found.");
                    }
                }
            }
        }
    }
}
