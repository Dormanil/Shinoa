// <copyright file="AnilistService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services.TimedServices
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;
    using Newtonsoft.Json;

    [Config("anilist")]
    public class AnilistService : ITimedService
    {
        private HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://anilist.co/api/anime/search/") };
        private string clientId;
        private string clientSecret;
        private string accessToken = string.Empty;
        private Color moduleColor = new Color(2, 169, 255);

        public async Task<Embed> GetEmbed(string query)
        {
            return (await GetAnime(query))?.GetEmbed(moduleColor) ??
                   new EmbedBuilder().AddField(f => f.WithName("Error").WithValue("Anime not found"))
                       .WithColor(moduleColor);
        }

        async Task ITimedService.Callback()
        {
            var client = new HttpClient();

            var postContent = new FormUrlEncodedContent(new[]
            {
                ("grant_type", "client_credentials").ToKeyValuePair(),
                ("client_id", clientId).ToKeyValuePair(),
                ("client_secret", clientSecret).ToKeyValuePair(),
            });

            var responseString = await (await client.PostAsync("https://anilist.co/api/auth/access_token", postContent)).Content.ReadAsStringAsync();
            dynamic responseObject = JsonConvert.DeserializeObject(responseString);
            accessToken = responseObject.access_token;
        }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            clientId = config["client_id"];
            clientSecret = config["client_secret"];

            try
            {
                moduleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));
            }
            catch (KeyNotFoundException)
            {
                Logging.LogError(
                        "AnilistService.Init: The property was not found on the dynamic object. No colors were supplied.")
                    .Wait();
            }
            catch (Exception e)
            {
                Logging.LogError(e.ToString()).Wait();
            }
        }

        private async Task<AnimeResult> GetAnime(string query)
        {
            if (query == "grape")
            {
                Logging.LogError($"Could not find anime \"{query}\"").Wait();
                return null;
            }

            try
            {
                var responseText = await (await httpClient.GetAsync($"{query}?access_token={accessToken}")).Content.ReadAsStringAsync();
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                dynamic firstResult = responseObject[0];

                var result = new AnimeResult
                {
                    Id = firstResult["id"],
                    JpTitle = firstResult["title_japanese"],
                    RomanizedTitle = firstResult["title_romaji"],
                    EnglishTitle = firstResult["title_english"],
                };

                if (firstResult["synonyms"].Count > 0)
                {
                    foreach (var synonym in firstResult["synonyms"])
                    {
                        result.Synonyms.Add((string)synonym);
                    }
                }

                if (firstResult["genres"].Count > 0)
                {
                    foreach (var genre in firstResult["genres"])
                    {
                        result.Genres.Add((string)genre);
                    }
                }

                result.Status = firstResult["airing_status"];
                result.Type = firstResult["type"];
                result.AverageScore = firstResult["average_score"];
                result.TotalEpisodes = firstResult["total_episodes"];
                result.Duration = firstResult["duration"] ?? 0;
                result.SourceMaterial = firstResult["source"];
                result.Description = ((string)firstResult["description"]).Truncate(500);
                result.Description = Regex.Replace(result.Description, @"\<.+?\>", string.Empty);
                result.ThumbnailUrl = firstResult["image_url_med"];

                result.StartDate = DateTime.Parse((string)firstResult["start_date"]);
                DateTime.TryParse((string)firstResult["end_date"], out var resultEndDate);
                result.EndDate = resultEndDate;

                return result;
            }
            catch (Exception)
            {
                Logging.LogError($"Could not find anime \"{query}\"").Wait();
                return null;
            }
        }

        private class AnimeResult
        {
            public int Id { get; set; }

            public string JpTitle { get; set; }

            public string RomanizedTitle { get; set; }

            public string EnglishTitle { get; set; }

            public List<string> Synonyms { get; } = new List<string>();

            public List<string> Genres { get; } = new List<string>();

            public string Type { get; set; }

            public string Status { get; set; }

            public float AverageScore { get; set; }

            public int TotalEpisodes { get; set; }

            public int Duration { get; set; }

            public string SourceMaterial { get; set; }

            public string ThumbnailUrl { get; set; }

            public string Description { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime EndDate { get; set; }

            public Embed GetEmbed(Color color)
            {
                var embed = new EmbedBuilder
                {
                    Title = RomanizedTitle,
                    Url = $"http://anilist.co/anime/{Id}",
                    ThumbnailUrl = ThumbnailUrl,
                };
                if (EnglishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(EnglishTitle).WithIsInline(true));
                if (JpTitle != null) embed.AddField(f => f.WithName("Japanese Title").WithValue(JpTitle).WithIsInline(true));

                Synonyms.RemoveAll(s => s == string.Empty);
                if (Synonyms.Count > 0)
                {
                    var synonymsString = string.Empty;
                    foreach (var synonym in Synonyms) synonymsString += synonym + ", ";
                    synonymsString = synonymsString.TrimEnd(',', ' ');
                    embed.AddField(f => f.WithName("Synonyms").WithValue(synonymsString));
                }

                embed.AddField(f => f.WithName("Type").WithValue(Type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(Status).WithIsInline(true));

                embed.AddField(f => f.WithName("Avg. Score (max. 100)").WithValue(AverageScore == 0 ? "?" : AverageScore.ToString()).WithIsInline(true));
                embed.AddField(f => f.WithName("Episodes").WithValue(TotalEpisodes == 0 ? "?" : TotalEpisodes.ToString()).WithIsInline(true));

                embed.AddField(f => f.WithName("Source Material").WithValue(SourceMaterial ?? "Unknown").WithIsInline(true));
                embed.AddField(f => f.WithName("Duration").WithValue(Duration == 0 ? "?" : $"{Duration} min.").WithIsInline(true));

                embed.AddField(f => f.WithName("Start Date").WithValue(StartDate.ToString("MMMM dd, yyyy")).WithIsInline(true));
                embed.AddField(f => f.WithName("End Date").WithValue(EndDate.Year == 1 ? "?" : EndDate.ToString("MMMM dd, yyyy")).WithIsInline(true));

                Genres.RemoveAll(s => s == string.Empty);
                if (Genres.Count > 0)
                {
                    var genresString = string.Empty;
                    foreach (var genre in Genres) genresString += genre + ", ";
                    genresString = genresString.TrimEnd(',', ' ');
                    embed.AddField(f => f.WithName("Genres").WithValue(genresString));
                }

                embed.AddField(f => f.WithName("Description").WithValue(Description));
                embed.WithFooter(f => f.WithText("Source: AniList"));
                embed.WithColor(color);
                return embed.Build();
            }
        }
    }
}
