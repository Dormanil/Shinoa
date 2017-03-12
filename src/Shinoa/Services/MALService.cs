using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Discord;
using Discord.Commands;
using Microsoft.CSharp.RuntimeBinder;
using Shinoa.Attributes;

namespace Shinoa.Services
{
    [Config("mal")]
    public class MALService : IService
    {
        public class AnimeResult
        {
            public int id;
            public string title;
            public string englishTitle;
            public string synonyms;
            public string type;
            public string status;
            public float score;
            public int episodeCount;
            public DateTime startDate;
            public DateTime endDate;
            public string thumbnailUrl;
            public string synopsis;

            public Embed GetEmbed(Color color)
            {
                var embed = new EmbedBuilder()
                {
                    Title = title,
                    Url = $"\nhttp://myanimelist.net/anime/{id}"
                };
                if (englishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(englishTitle));
                if (synonyms != null) embed.AddField(f => f.WithName("Synonyms").WithValue(synonyms));
                embed.AddField(f => f.WithName("Type").WithValue(type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(status).WithIsInline(true));
                embed.AddField(f => f.WithName("Score (max. 10)").WithValue(score.ToString()).WithIsInline(true));
                embed.AddField(
                    f =>
                        f.WithName("Episode Count")
                            .WithValue(episodeCount == 0 ? "?" : episodeCount.ToString())
                            .WithIsInline(true));
                embed.AddField(
                    f =>
                        f.WithName("Start Date")
                            .WithValue(startDate.ToString("MMMM dd, yyyy"))
                            .WithIsInline(true)
                            .WithIsInline(true));

                if (endDate != null && endDate.Year != 1)
                {
                    if (startDate != endDate)
                        embed.AddField(
                            f => f.WithName("End Date").WithValue(endDate.ToString("MMMM dd, yyyy")).WithIsInline(true));
                    else
                        embed.AddField(f => f.WithName("End Date").WithValue("N/A").WithIsInline(true));
                }
                else
                {
                    embed.AddField(f => f.WithName("End Date").WithValue("?").WithIsInline(true));
                }

                embed.ThumbnailUrl = thumbnailUrl;
                embed.AddField(f => f.WithName("Synopsis").WithValue(synopsis));
                embed.WithFooter(f => f.WithText("Source: MyAnimeList"));
                embed.WithColor(color);
                return embed.Build();
            }
        }

        public class MangaResult
        {
            public int id;
            public string title;
            public string englishTitle;
            public string synonyms;
            public string type;
            public string status;
            public float score;
            public int? chapterCount;
            public int? volumeCount;
            public string dateString;
            public string thumbnailUrl;
            public string synopsis;

            public Embed GetEmbed(Color color)
            {
                var embed = new EmbedBuilder()
                {
                    Title = title,
                    Url = $"\nhttp://myanimelist.net/manga/{id}"
                };

                //embed.AddField(f => f.WithName("Title").WithValue(title));
                if (englishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(englishTitle));
                if (synonyms != null) embed.AddField(f => f.WithName("Synonyms").WithValue(synonyms));
                embed.AddField(f => f.WithName("Type").WithValue(type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(status).WithIsInline(true));
                embed.AddField(f => f.WithName("Score (max. 10)").WithValue(score.ToString()).WithIsInline(true));

                if (chapterCount.HasValue)
                    embed.AddField(f => f.WithName("Chapters").WithValue(chapterCount.ToString()).WithIsInline(true));
                else
                    embed.AddField(f => f.WithName("Chapters").WithValue("?").WithIsInline(true));

                if (volumeCount.HasValue)
                    embed.AddField(f => f.WithName("Volumes").WithValue(volumeCount.ToString()).WithIsInline(true));
                else
                    embed.AddField(f => f.WithName("Volumes").WithValue("?").WithIsInline(true));

                embed.AddField(f => f.WithName("Published").WithValue(dateString).WithIsInline(true));
                //embed.AddField(f => f.WithName("MyAnimeList Page").WithValue($"\nhttp://myanimelist.net/manga/{id}"));
                embed.ThumbnailUrl = thumbnailUrl;
                embed.AddField(f => f.WithName("Synopsis").WithValue(synopsis));
                embed.WithFooter(f => f.WithText("Source: MyAnimeList"));
                embed.WithColor(color);
                return embed.Build();
            }
        }

        private HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://myanimelist.net/api/") };
        public Color ModuleColor { get; private set; }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            httpClient.SetBasicHttpCredentials((string)config["username"], (string)config["password"]);

            ModuleColor = new Color(63, 81, 181);
            try
            {
                ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]),
                    byte.Parse(config["color"][2]));
            }
            catch (KeyNotFoundException)
            {
                Logging.LogError(
                        "MALService.Init: The property was not found on the dynamic object. No colors were supplied.")
                    .Wait();
            }
            catch(Exception e)
            {
                Logging.LogError(e.ToString()).Wait();
            }
        }


        static string GenerateSynopsisString(string rawValue)
        {
            var synopsisString = System.Net.WebUtility.HtmlDecode(rawValue)
                .Replace("<br />", "");

            if (synopsisString.ParagraphCount() > 1)
            {
                synopsisString = synopsisString.FirstParagraph();
                synopsisString += "\n\n(...)";
            }

            synopsisString = Regex.Replace(synopsisString, @"\[.+?\]", "");
            if (synopsisString.Length >= 500)
            {
                synopsisString = synopsisString.Truncate(500);

            }
            return synopsisString;
        }

        public AnimeResult GetAnime(string searchQuery)
        {
            try
            {
                var result = new AnimeResult();

                var responseText = httpClient.HttpGet($"anime/search.xml?q={searchQuery}");
                XElement root = XElement.Parse(responseText);
                var firstResult = (from el in root.Descendants("entry") select el).First();

                result.title = firstResult.Descendants("title").First().Value;

                var englishTitle = firstResult.Descendants("english").First().Value;
                if (englishTitle.Length > 0) result.englishTitle = englishTitle;

                var synonyms = firstResult.Descendants("synonyms").First().Value;
                if (synonyms.Length > 0) result.synonyms = synonyms;

                result.type = firstResult.Descendants("type").First().Value;
                result.status = firstResult.Descendants("status").First().Value;
                result.score = float.Parse(firstResult.Descendants("score").First().Value);
                result.episodeCount = int.Parse(firstResult.Descendants("episodes").First().Value);

                result.startDate = DateTime.Parse(firstResult.Descendants("start_date").First().Value);
                var endDateString = firstResult.Descendants("end_date").First().Value;

                if (endDateString != "0000-00-00")
                {
                    result.endDate = DateTime.Parse(endDateString);
                }

                result.id = int.Parse(firstResult.Descendants("id").First().Value);
                result.thumbnailUrl = firstResult.Descendants("image").First().Value;
                result.synopsis = GenerateSynopsisString(firstResult.Descendants("synopsis").First().Value);
                return result;
            }
            catch (Exception)
            {
                Logging.LogError($"Could not find anime \"{searchQuery}\"").Wait();
                return null;
            }
        }

        public MangaResult GetManga(string searchQuery)
        {
            try
            {
                var result = new MangaResult();

                var responseText = httpClient.HttpGet($"manga/search.xml?q={searchQuery}");
                XElement root = XElement.Parse(responseText);
                var firstResult = (from el in root.Descendants("entry") select el).First();

                result.title = firstResult.Descendants("title").First().Value;

                var englishTitle = firstResult.Descendants("english").First().Value;
                if (englishTitle.Length > 0) result.englishTitle = englishTitle;

                var synonyms = firstResult.Descendants("synonyms").First().Value;
                if (synonyms.Length > 0) result.synonyms = synonyms;

                result.type = firstResult.Descendants("type").First().Value;
                result.status = firstResult.Descendants("status").First().Value;
                result.score = float.Parse(firstResult.Descendants("score").First().Value);

                var chaptersString = firstResult.Descendants("chapters").First().Value;
                var volumesString = firstResult.Descendants("volumes").First().Value;
                if (chaptersString != "0") result.chapterCount = int.Parse(chaptersString);
                if (volumesString != "0") result.volumeCount = int.Parse(volumesString);

                var startDate = DateTime.Parse(firstResult.Descendants("start_date").First().Value).ToString("MMMM dd, yyyy");
                var endDate = firstResult.Descendants("end_date").First().Value;
                if (endDate == "0000-00-00") endDate = "?";
                else endDate = DateTime.Parse(endDate).ToString("MMMM dd, yyyy");
                result.dateString = $"{startDate} -> {endDate}\n";

                result.id = int.Parse(firstResult.Descendants("id").First().Value);
                result.thumbnailUrl = firstResult.Descendants("image").First().Value;
                result.synopsis = GenerateSynopsisString(firstResult.Descendants("synopsis").First().Value);
                return result;
            }
            catch (Exception)
            {
                Logging.LogError($"Could not find manga \"{searchQuery}\"").Wait();
                return null;
            }
        }
    }
}
