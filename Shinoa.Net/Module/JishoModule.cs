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
    class JishoModule : IModule
    {
        static RestClient RestClient = new RestClient("http://jisho.org/api/v1/");

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
                var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"jp (?<querytext>.*)");
                if (regex.IsMatch(e.Message.Text))
                {
                    var queryText = regex.Matches(e.Message.Text)[0].Groups["querytext"];

                    var request = new RestRequest($"search/words?keyword={queryText}");

                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);
                    }

                    dynamic responseObject = JsonConvert.DeserializeObject(response.Content);

                    Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] {e.User.Name} searched Jisho for '{queryText}'.");

                    try
                    {
                        dynamic firstResult = responseObject["data"][0];

                        var responseText = "";

                        foreach(var word in firstResult["japanese"])
                        {
                            var wordKanji = word["word"];
                            var wordReading = word["reading"];

                            if (wordKanji != null && wordReading != null) responseText += $"**{wordKanji}** - {wordReading}, ";
                            else if (wordKanji != null) responseText += $"**{wordKanji}**, ";
                            else if (wordReading != null) responseText += $"**{wordReading}**, ";
                        }

                        responseText = responseText.Trim(new char[] { ',', ' ' });
                        responseText += '\n';

                        foreach (var sense in firstResult["senses"])
                        {
                            responseText += "\u2022 ";

                            foreach (var definition in sense["english_definitions"])
                            {
                                responseText += $"{definition}, ";
                            }

                            responseText = responseText.Trim(new char[] { ',', ' ' });
                            responseText += '\n';
                        }

                        responseText += $"\nSee more: <http://jisho.org/search/{System.Uri.EscapeUriString(queryText.Value)}>";

                        e.Channel.SendMessage(responseText);
                    }
                    catch (RuntimeBinderException ex)
                    {
                        e.Channel.SendMessage("No results.");
                    }
                    catch (Exception ex)
                    {
                        e.Channel.SendMessage("Error encountered, not found.");
                        Logging.Log(ex.ToString());
                    }
                }
            }
        }
    }
}
