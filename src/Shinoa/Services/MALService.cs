// <copyright file="MALService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using Attributes;
    using Discord;
    using Discord.Commands;

    [Config("mal")]
    public class MalService : IService
    {
        private readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://myanimelist.net/api/") };

        public Color ModuleColor { get; private set; }

        public AnimeResult GetAnime(string searchQuery)
        {
            if (searchQuery == "grape")
            {
                Logging.LogError($"Could not find anime \"{searchQuery}\"").Wait();
                return null;
            }

            try
            {
                var result = new AnimeResult();

                var responseText = httpClient.HttpGet($"anime/search.xml?q={searchQuery}").Result;
                if (responseText == null)
                {
                    Logging.LogError($"Could not find anime \"{searchQuery}\"").Wait();
                    return null;
                }

                var root = XElement.Parse(responseText);
                var firstResult = (from el in root.Descendants("entry") select el).First();

                result.Title = firstResult.Descendants("title").First().Value;

                var englishTitle = firstResult.Descendants("english").First().Value;
                if (englishTitle.Length > 0) result.EnglishTitle = englishTitle;

                var synonyms = firstResult.Descendants("synonyms").First().Value;
                if (synonyms.Length > 0) result.Synonyms = synonyms;

                result.Type = firstResult.Descendants("type").First().Value;
                result.Status = firstResult.Descendants("status").First().Value;
                result.Score = float.Parse(firstResult.Descendants("score").First().Value);
                result.EpisodeCount = int.Parse(firstResult.Descendants("episodes").First().Value);

                var startDateString = firstResult.Descendants("start_date").First().Value;

                if (startDateString != "0000-00-00")
                    result.StartDate = DateTime.Parse(startDateString);

                var endDateString = firstResult.Descendants("end_date").First().Value;

                if (endDateString != "0000-00-00")
                    result.EndDate = DateTime.Parse(endDateString);

                result.Id = int.Parse(firstResult.Descendants("id").First().Value);
                result.ThumbnailUrl = firstResult.Descendants("image").First().Value;
                result.Synopsis = GenerateSynopsisString(firstResult.Descendants("synopsis").First().Value);
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

                var responseText = httpClient.HttpGet($"manga/search.xml?q={searchQuery}").Result;
                if (responseText == null)
                {
                    Logging.LogError($"Could not find manga \"{searchQuery}\"").Wait();
                    return null;
                }

                var root = XElement.Parse(responseText);
                var firstResult = (from el in root.Descendants("entry") select el).First();

                result.Title = firstResult.Descendants("title").First().Value;

                var englishTitle = firstResult.Descendants("english").First().Value;
                if (englishTitle.Length > 0) result.EnglishTitle = englishTitle;

                var synonyms = firstResult.Descendants("synonyms").First().Value;
                if (synonyms.Length > 0) result.Synonyms = synonyms;

                result.Type = firstResult.Descendants("type").First().Value;
                result.Status = firstResult.Descendants("status").First().Value;
                result.Score = float.Parse(firstResult.Descendants("score").First().Value);

                var chaptersString = firstResult.Descendants("chapters").First().Value;
                var volumesString = firstResult.Descendants("volumes").First().Value;
                if (chaptersString != "0") result.ChapterCount = int.Parse(chaptersString);
                if (volumesString != "0") result.VolumeCount = int.Parse(volumesString);

                var startDate = DateTime.Parse(firstResult.Descendants("start_date").First().Value).ToString("MMMM dd, yyyy");
                var endDate = firstResult.Descendants("end_date").First().Value;
                endDate = endDate == "0000-00-00" ? "?" : DateTime.Parse(endDate).ToString("MMMM dd, yyyy");
                result.DateString = $"{startDate} -> {endDate}\n";

                result.Id = int.Parse(firstResult.Descendants("id").First().Value);
                result.ThumbnailUrl = firstResult.Descendants("image").First().Value;
                result.Synopsis = GenerateSynopsisString(firstResult.Descendants("synopsis").First().Value);
                return result;
            }
            catch (Exception)
            {
                Logging.LogError($"Could not find manga \"{searchQuery}\"").Wait();
                return null;
            }
        }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            httpClient.SetBasicHttpCredentials((string)config["username"], (string)config["password"]);

            ModuleColor = new Color(63, 81, 181);
            try
            {
                ModuleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));
            }
            catch (KeyNotFoundException)
            {
                Logging.LogError("MALService.Init: The property was not found on the dynamic object. No colors were supplied.")
                    .Wait();
            }
            catch (Exception e)
            {
                Logging.LogError(e.ToString()).Wait();
            }
        }

        private static string GenerateSynopsisString(string rawValue)
        {
            var synopsisString = WebUtility.HtmlDecode(rawValue)
                .Replace("<br />", string.Empty);

            if (synopsisString.ParagraphCount() > 1)
            {
                synopsisString = synopsisString.FirstParagraph();
                synopsisString += "\n\n(...)";
            }

            synopsisString = Regex.Replace(synopsisString, @"\[.+?\]", string.Empty);
            if (synopsisString.Length >= 500)
                synopsisString = synopsisString.Truncate(500);

            return synopsisString;
        }

        public class AnimeResult
        {
            public int Id { get; set; }

            public string Title { get; set; }

            public string EnglishTitle { get; set; }

            public string Synonyms { get; set; }

            public string Type { get; set; }

            public string Status { get; set; }

            public float Score { get; set; }

            public int EpisodeCount { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime EndDate { get; set; }

            public string ThumbnailUrl { get; set; }

            public string Synopsis { get; set; }

            public Embed GetEmbed(Color color)
            {
                var embed = new EmbedBuilder
                {
                    Title = Title,
                    Url = $"\nhttp://myanimelist.net/anime/{Id}",
                };
                if (EnglishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(EnglishTitle));
                if (Synonyms != null) embed.AddField(f => f.WithName("Synonyms").WithValue(Synonyms));
                embed.AddField(f => f.WithName("Type").WithValue(Type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(Status).WithIsInline(true));
                embed.AddField(f => f.WithName("Score (max. 10)").WithValue(Score.ToString()).WithIsInline(true));
                embed.AddField(f => f.WithName("Episode Count").WithValue(EpisodeCount == 0 ? "?" : EpisodeCount.ToString()).WithIsInline(true));
                if (StartDate.Year != 1)
                {
                    embed.AddField(f => f.WithName("Start Date").WithValue(StartDate.ToString("MMMM dd, yyyy")).WithIsInline(true));

                    if (EndDate.Year != 1)
                    {
                        if (StartDate != EndDate)
                            embed.AddField(f => f.WithName("End Date").WithValue(EndDate.ToString("MMMM dd, yyyy")).WithIsInline(true));
                        else
                            embed.AddField(f => f.WithName("End Date").WithValue("N/A").WithIsInline(true));
                    }
                    else
                    {
                        embed.AddField(f => f.WithName("End Date").WithValue("?").WithIsInline(true));
                    }
                }
                else
                {
                    embed.AddField(f => f.WithName("Start Date").WithValue("?").WithIsInline(true));
                    embed.AddField(f => f.WithName("End Date").WithValue("?").WithIsInline(true));
                }

                embed.ThumbnailUrl = ThumbnailUrl;
                embed.AddField(f => f.WithName("Synopsis").WithValue(Synopsis));
                embed.WithFooter(f => f.WithText("Source: MyAnimeList"));
                embed.WithColor(color);
                return embed.Build();
            }
        }

        public class MangaResult
        {
            public int Id { get; set; }

            public string Title { get; set; }

            public string EnglishTitle { get; set; }

            public string Synonyms { get; set; }

            public string Type { get; set; }

            public string Status { get; set; }

            public float Score { get; set; }

            public int? ChapterCount { get; set; }

            public int? VolumeCount { get; set; }

            public string DateString { get; set; }

            public string ThumbnailUrl { get; set; }

            public string Synopsis { get; set; }

            public Embed GetEmbed(Color color)
            {
                var embed = new EmbedBuilder
                {
                    Title = Title,
                    Url = $"\nhttp://myanimelist.net/manga/{Id}",
                };

                ////embed.AddField(f => f.WithName("Title").WithValue(title));
                if (EnglishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(EnglishTitle));
                if (Synonyms != null) embed.AddField(f => f.WithName("Synonyms").WithValue(Synonyms));
                embed.AddField(f => f.WithName("Type").WithValue(Type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(Status).WithIsInline(true));
                embed.AddField(f => f.WithName("Score (max. 10)").WithValue(Score.ToString()).WithIsInline(true));

                if (ChapterCount.HasValue)
                    embed.AddField(f => f.WithName("Chapters").WithValue(ChapterCount.ToString()).WithIsInline(true));
                else
                    embed.AddField(f => f.WithName("Chapters").WithValue("?").WithIsInline(true));

                if (VolumeCount.HasValue)
                    embed.AddField(f => f.WithName("Volumes").WithValue(VolumeCount.ToString()).WithIsInline(true));
                else
                    embed.AddField(f => f.WithName("Volumes").WithValue("?").WithIsInline(true));

                embed.AddField(f => f.WithName("Published").WithValue(DateString).WithIsInline(true));
                ////embed.AddField(f => f.WithName("MyAnimeList Page").WithValue($"\nhttp://myanimelist.net/manga/{id}"));
                embed.ThumbnailUrl = ThumbnailUrl;
                embed.AddField(f => f.WithName("Synopsis").WithValue(Synopsis));
                embed.WithFooter(f => f.WithText("Source: MyAnimeList"));
                embed.WithColor(color);
                return embed.Build();
            }
        }
    }
}
