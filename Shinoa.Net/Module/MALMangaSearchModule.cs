using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;
using System.Xml;
using RestSharp.Authenticators;
using System.Xml.Linq;
using System.Net;
using System.IO;

namespace Shinoa.Net.Module
{
    class MALMangaSearchModule : IModule
    {
        static RestClient RestClient = new RestClient("http://myanimelist.net/api/");

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            RestClient.Authenticator = new HttpBasicAuthenticator(ShinoaNet.Config["mal_username"], ShinoaNet.Config["mal_password"]);
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                var regex = new Regex(@"^!manga (?<querytext>.*)");
                if (regex.IsMatch(e.Message.Text))
                {
                    var queryText = regex.Matches(e.Message.Text)[0].Groups["querytext"];

                    Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] {e.User.Name} searched MAL (Manga) for '{queryText}'.");

                    var request = new RestRequest($"manga/search.xml?q={queryText}");

                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);
                    }

                    try
                    {
                        XElement root = XElement.Parse(response.Content);

                        var firstResult = (from el in root.Descendants("entry") select el).First();

                        var responseMessage = "";
                        responseMessage += $"Title: **{firstResult.Descendants("title").First().Value}**\n";

                        var englishTitle = firstResult.Descendants("english").First().Value;
                        if (englishTitle.Length > 0) responseMessage += $"English title: **{englishTitle}**\n";

                        var synonyms = firstResult.Descendants("synonyms").First().Value;
                        if (synonyms.Length > 0) responseMessage += $"Synonyms: {synonyms}\n";

                        responseMessage += "\n";

                        responseMessage += $"Type: {firstResult.Descendants("type").First().Value}\n";
                        responseMessage += $"Status: {firstResult.Descendants("status").First().Value}\n";
                        responseMessage += $"Average score (max 10): {firstResult.Descendants("score").First().Value}\n";
                        responseMessage += $"Chapters: {firstResult.Descendants("chapters").First().Value}\n";
                        responseMessage += $"Volumes: {firstResult.Descendants("volumes").First().Value}\n";

                        var startDate = firstResult.Descendants("start_date").First().Value;
                        var endDate = firstResult.Descendants("end_date").First().Value;
                        if (endDate == "0000-00-00") endDate = "?";
                        responseMessage += $"Aired: {startDate} -> {endDate}\n";

                        responseMessage += $"\nhttp://myanimelist.net/manga/{firstResult.Descendants("id").First().Value}";

                        e.Channel.SendMessage(responseMessage);
                    }
                    catch (Exception ex)
                    {
                        e.Channel.SendMessage("Manga not found.");
                        Logging.Log(ex.ToString());
                    }
                }
            }
        }
    }
}
