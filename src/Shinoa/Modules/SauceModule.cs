using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class SauceModule: Abstract.HttpClientModule
    {
        public override void Init()
        {
            this.BoundCommands.Add("sauce", (e) =>
            {
                var responseMessage = e.Channel.SendMessage("Searching...").Result;

                var imageUrl = "";

                if (e.Message.Text.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Length == 1)
                {
                    if (e.Message.Attachments.Length > 0)
                    {
                        imageUrl = e.Message.Attachments[0].Url;
                    }
                    else
                    {
                        foreach (var message in e.Channel.Messages)
                        {
                            if (message.Embeds.Length > 0)
                            {
                                imageUrl = message.Embeds[0].Url;
                                break;
                            }
                            else if (message.Attachments.Length > 0)
                            {
                                imageUrl = message.Attachments[0].Url;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    imageUrl = GetCommandParameters(e.Message.Text)[0];
                }

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("urlify", "on"),
                    new KeyValuePair<string, string>("url", imageUrl),
                    new KeyValuePair<string, string>("frame", "1"),
                    new KeyValuePair<string, string>("hide", "0"),
                    new KeyValuePair<string, string>("database", "999")
                });
                
                var resultPageHtml = HttpPost("https://saucenao.com/search.php", content);

                var document = new HtmlDocument();
                document.LoadHtml(resultPageHtml);
                var percentage = document.DocumentNode.SelectNodes(@"//div[@class='resultsimilarityinfo']")[0].InnerHtml;
                var pixivLink = document.DocumentNode.SelectNodes(@"//div[@class='resultcontentcolumn']/a[@class='linkify']")[0].Attributes["href"].Value;

                responseMessage.Edit($"This is the closest match, with **{percentage}** similarity:\n{pixivLink}");
            });
        }
    }
}
