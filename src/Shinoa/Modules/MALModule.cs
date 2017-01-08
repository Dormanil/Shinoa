using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Xml.Linq;
using System.Threading;
using System.Net.Http;
using Discord.Commands;
using Shinoa.Attributes;

namespace Shinoa.Modules
{
    public class MALModule : Abstract.Module
    {
        public static Color MODULE_COLOR = new Color(63, 81, 181);
        HttpClient httpClient = new HttpClient();

        public override void Init()
        {
            httpClient.SetBasicHttpCredentials((string)Shinoa.Config["mal_username"], (string)Shinoa.Config["mal_password"]);
            httpClient.BaseAddress = new Uri("https://myanimelist.net/api/");
        }

        [@Command("anime", "mal", "malanime")]
        public void MALAnimeSearch(CommandContext c, params string[] args)
        {
            var queryText = args.ToRemainderString();
            var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;

            try
            {
                var responseText = httpClient.HttpGet($"anime/search.xml?q={queryText}");

                XElement root = XElement.Parse(responseText);

                var firstResult = (from el in root.Descendants("entry") select el).First();

                var embed = new EmbedBuilder();
                embed.AddField(f => f.WithName("Title").WithValue(firstResult.Descendants("title").First().Value));

                var englishTitle = firstResult.Descendants("english").First().Value;
                if (englishTitle.Length > 0) embed.AddField(f => f.WithName("English Title").WithValue(englishTitle));

                var synonyms = firstResult.Descendants("synonyms").First().Value;
                if (synonyms.Length > 0) embed.AddField(f => f.WithName("Synonyms").WithValue(synonyms));

                embed.AddField(f => f.WithName("Type").WithValue(firstResult.Descendants("type").First().Value).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(firstResult.Descendants("status").First().Value).WithIsInline(true));
                embed.AddField(f => f.WithName("Score (max. 10)").WithValue(firstResult.Descendants("score").First().Value).WithIsInline(true));
                embed.AddField(f => f.WithName("Episode Count").WithValue(firstResult.Descendants("episodes").First().Value).WithIsInline(true));

                var startDate = DateTime.Parse(firstResult.Descendants("start_date").First().Value);
                var endDateString = firstResult.Descendants("end_date").First().Value;
                DateTime endDate = DateTime.Parse(endDateString);

                embed.AddField(f => f.WithName("Start Date").WithValue(startDate.ToString("MMMM dd, yyyy")).WithIsInline(true));

                if (endDateString != "0000-00-00")
                {
                    if (startDate != endDate)
                    {
                        embed.AddField(f => f.WithName("End Date").WithValue(endDate.ToString("MMMM dd, yyyy")).WithIsInline(true));
                    }
                    else
                    {
                        embed.AddField(f => f.WithName("End Date").WithValue("N/A").WithIsInline(true));
                    }
                }
                else
                {
                    embed.AddField(f => f.WithName("End Date").WithValue("?").WithIsInline(true));
                }

                embed.AddField(f => f.WithName("MyAnimeList Page").WithValue($"\nhttp://myanimelist.net/anime/{firstResult.Descendants("id").First().Value}"));
                embed.ThumbnailUrl = firstResult.Descendants("image").First().Value;

                var synopsisString = System.Net.WebUtility.HtmlDecode(firstResult.Descendants("synopsis").First().Value)
                    .Replace("<br />", "").Truncate(1000);
                embed.AddField(f => f.WithName("Synopsis").WithValue(synopsisString));

                embed.WithFooter(f => f.WithText("Source: MyAnimeList"));
                embed.WithColor(MODULE_COLOR);

                responseMessage.ModifyToEmbedAsync(embed.Build());
            }
            catch (Exception)
            {
                responseMessage.ModifyAsync(p => p.Content = "Anime not found.");
            }
        }

        [@Command("manga", "ln", "malmanga")]
        public void MALMangaSearch(CommandContext c, params string[] args)
        {
            var queryText = args.ToRemainderString();
            var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;

            try
            {
                var responseText = httpClient.HttpGet($"manga/search.xml?q={queryText}");

                XElement root = XElement.Parse(responseText);

                var firstResult = (from el in root.Descendants("entry") select el).First();

                var embed = new EmbedBuilder();
                embed.AddField(f => f.WithName("Title").WithValue(firstResult.Descendants("title").First().Value));

                var englishTitle = firstResult.Descendants("english").First().Value;
                if (englishTitle.Length > 0) embed.AddField(f => f.WithName("English Title").WithValue(englishTitle));

                var synonyms = firstResult.Descendants("synonyms").First().Value;
                if (synonyms.Length > 0) embed.AddField(f => f.WithName("Synonyms").WithValue(synonyms));

                var chaptersString = firstResult.Descendants("chapters").First().Value;
                var volumesString = firstResult.Descendants("volumes").First().Value;
                if (chaptersString == "0") chaptersString = "?";
                if (volumesString == "0") volumesString = "?";

                embed.AddField(f => f.WithName("Type").WithValue(firstResult.Descendants("type").First().Value).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(firstResult.Descendants("status").First().Value).WithIsInline(true));
                embed.AddField(f => f.WithName("Score (max. 10)").WithValue(firstResult.Descendants("score").First().Value).WithIsInline(true));
                embed.AddField(f => f.WithName("Chapters").WithValue(chaptersString).WithIsInline(true));
                embed.AddField(f => f.WithName("Volumes").WithValue(volumesString).WithIsInline(true));

                var startDate = firstResult.Descendants("start_date").First().Value;
                var endDate = firstResult.Descendants("end_date").First().Value;
                if (endDate == "0000-00-00") endDate = "?";
                var dateString = $"{startDate} -> {endDate}\n";

                embed.AddField(f => f.WithName("Published").WithValue(dateString).WithIsInline(true));

                embed.AddField(f => f.WithName("MyAnimeList Page").WithValue($"\nhttp://myanimelist.net/manga/{firstResult.Descendants("id").First().Value}"));
                embed.ThumbnailUrl = firstResult.Descendants("image").First().Value;

                var synopsisString = System.Net.WebUtility.HtmlDecode(firstResult.Descendants("synopsis").First().Value)
                    .Replace("<br />", "").Truncate(1000);
                embed.AddField(f => f.WithName("Synopsis").WithValue(synopsisString));

                embed.WithFooter(f => f.WithText("Source: MyAnimeList"));
                embed.WithColor(MODULE_COLOR);

                responseMessage.ModifyToEmbedAsync(embed.Build());
            }
            catch (Exception)
            {
                responseMessage.ModifyAsync(p => p.Content = "Manga not found.");
            }
        }
    }
}
