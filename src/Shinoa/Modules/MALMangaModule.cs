using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Xml.Linq;

namespace Shinoa.Modules
{
    public class MALMangaModule : Abstract.HttpClientModule
    {
        public override void Init()
        {
            this.SetBasicHttpCredentials(Shinoa.Config["mal_username"], Shinoa.Config["mal_password"]);
            this.BaseUrl = "https://myanimelist.net/api/";

            this.BoundCommands.Add("manga", (e) =>
            {
                var queryText = GetCommandParametersAsString(e.Message.Text);

                try
                {
                    var responseText = HttpGet($"manga/search.xml?q={queryText}");

                    XElement root = XElement.Parse(responseText);

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
                    responseMessage += $"Published: {startDate} -> {endDate}\n";

                    responseMessage += $"\nhttp://myanimelist.net/manga/{firstResult.Descendants("id").First().Value}";

                    e.Channel.SendMessage(responseMessage);
                }
                catch (Exception)
                {
                    e.Channel.SendMessage("Manga not found.");
                }
            });
        }
    }
}
