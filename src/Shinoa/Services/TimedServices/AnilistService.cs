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
            return (await this.GetAnime(query))?.GetEmbed(this.moduleColor) ??
                   new EmbedBuilder().AddField(f => f.WithName("Error").WithValue("Anime not found"))
                       .WithColor(this.moduleColor);
        }

        async Task ITimedService.Callback()
        {
            var client = new HttpClient();

            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", this.clientId),
                new KeyValuePair<string, string>("client_secret", this.clientSecret),
            });

            var responseString = await (await client.PostAsync("https://anilist.co/api/auth/access_token", postContent)).Content.ReadAsStringAsync();
            dynamic responseObject = JsonConvert.DeserializeObject(responseString);
            this.accessToken = responseObject.access_token;
        }

        void IService.Init(dynamic config, IDependencyMap map)
        {
            this.clientId = config["client_id"];
            this.clientSecret = config["client_secret"];

            try
            {
                this.moduleColor = new Color(byte.Parse(config["color"][0]), byte.Parse(config["color"][1]), byte.Parse(config["color"][2]));
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
            try
            {
                var client = new HttpClient();
                var responseText = await (await client.GetAsync($"{query}?access_token={this.accessToken}")).Content.ReadAsStringAsync();
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
                    Title = this.RomanizedTitle,
                    Url = $"http://anilist.co/anime/{this.Id}",
                    ThumbnailUrl = this.ThumbnailUrl,
                };
                if (this.EnglishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(this.EnglishTitle).WithIsInline(true));
                if (this.JpTitle != null) embed.AddField(f => f.WithName("Japanese Title").WithValue(this.JpTitle).WithIsInline(true));

                this.Synonyms.RemoveAll(s => s == string.Empty);
                if (this.Synonyms.Count > 0)
                {
                    var synonymsString = string.Empty;
                    foreach (var synonym in this.Synonyms) synonymsString += synonym + ", ";
                    synonymsString = synonymsString.TrimEnd(',', ' ');
                    embed.AddField(f => f.WithName("Synonyms").WithValue(synonymsString));
                }

                embed.AddField(f => f.WithName("Type").WithValue(this.Type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(this.Status).WithIsInline(true));

                embed.AddField(f => f.WithName("Avg. Score (max. 100)").WithValue(this.AverageScore == 0 ? "?" : this.AverageScore.ToString()).WithIsInline(true));
                embed.AddField(f => f.WithName("Episodes").WithValue(this.TotalEpisodes == 0 ? "?" : this.TotalEpisodes.ToString()).WithIsInline(true));

                embed.AddField(f => f.WithName("Source Material").WithValue(this.SourceMaterial ?? "Unknown").WithIsInline(true));
                embed.AddField(f => f.WithName("Duration").WithValue(this.Duration == 0 ? "?" : $"{this.Duration} min.").WithIsInline(true));

                embed.AddField(f => f.WithName("Start Date").WithValue(this.StartDate.ToString("MMMM dd, yyyy")).WithIsInline(true));
                embed.AddField(f => f.WithName("End Date").WithValue(this.EndDate.Year == 1 ? "?" : this.EndDate.ToString("MMMM dd, yyyy")).WithIsInline(true));

                this.Genres.RemoveAll(s => s == string.Empty);
                if (this.Genres.Count > 0)
                {
                    var genresString = string.Empty;
                    foreach (var genre in this.Genres) genresString += genre + ", ";
                    genresString = genresString.TrimEnd(',', ' ');
                    embed.AddField(f => f.WithName("Genres").WithValue(genresString));
                }

                embed.AddField(f => f.WithName("Description").WithValue(this.Description));
                embed.WithFooter(f => f.WithText("Source: AniList"));
                embed.WithColor(color);
                return embed.Build();
            }
        }
    }
}
