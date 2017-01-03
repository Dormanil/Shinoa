using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Xml.Linq;
using System.Threading;

namespace Shinoa.Modules
{
    public class MALAnimeModule : Abstract.HttpClientModule
    {
        public override void Init()
        {
            this.SetBasicHttpCredentials(Shinoa.Config["mal_username"], Shinoa.Config["mal_password"]);
            this.BaseUrl = "https://myanimelist.net/api/";

            this.BoundCommands.Add("anime", (e) =>
            {
                var queryText = GetCommandParametersAsString(e.Message.Text);
                var responseMessage = e.Channel.SendMessage("Searching...").Result;

                try
                {
                    var responseText = HttpGet($"anime/search.xml?q={queryText}");

                    XElement root = XElement.Parse(responseText);

                    var firstResult = (from el in root.Descendants("entry") select el).First();

                    var resultMessage = "";
                    resultMessage += $"Title: **{firstResult.Descendants("title").First().Value}**\n";

                    var englishTitle = firstResult.Descendants("english").First().Value;
                    if (englishTitle.Length > 0) resultMessage += $"English title: **{englishTitle}**\n";

                    var synonyms = firstResult.Descendants("synonyms").First().Value;
                    if (synonyms.Length > 0) resultMessage += $"Synonyms: {synonyms}\n";

                    resultMessage += "\n";

                    resultMessage += $"Type: {firstResult.Descendants("type").First().Value}\n";
                    resultMessage += $"Status: {firstResult.Descendants("status").First().Value}\n";
                    resultMessage += $"Average score (max 10): {firstResult.Descendants("score").First().Value}\n";
                    resultMessage += $"Episode count: {firstResult.Descendants("episodes").First().Value}\n";

                    var startDate = firstResult.Descendants("start_date").First().Value;
                    var endDate = firstResult.Descendants("end_date").First().Value;
                    if (endDate == "0000-00-00") endDate = "?";
                    resultMessage += $"Aired: {startDate} -> {endDate}\n";

                    resultMessage += $"\nhttp://myanimelist.net/anime/{firstResult.Descendants("id").First().Value}";

                    responseMessage.Edit(resultMessage);
                }
                catch (Exception)
                {
                    responseMessage.Edit("Anime not found.");
                }
            });
        }
    }
}
