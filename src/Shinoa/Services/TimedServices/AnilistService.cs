using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Shinoa.Attributes;

namespace Shinoa.Services.TimedServices
{
    [Config("anilist")]
    public class AnilistService : ITimedService
    {
        class AnimeResult
        {
            public int id;
            public string jpTitle;
            public string romanizedTitle;
            public string englishTitle;
            public List<string> synonyms = new List<string>();
            public List<string> genres = new List<string>();
            public string type;
            public string status;
            public float averageScore;
            public int totalEpisodes;
            public int duration;
            public string sourceMaterial;
            public string thumbnailUrl;
            public string description;

            public DateTime startDate;
            public DateTime endDate;

            public Embed GetEmbed(Color color)
            {
                var embed = new EmbedBuilder
                {
                    Title = romanizedTitle,
                    Url = $"http://anilist.co/anime/{id}",
                    ThumbnailUrl = thumbnailUrl
                };
                if (englishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(englishTitle).WithIsInline(true));
                if (jpTitle != null) embed.AddField(f => f.WithName("Japanese Title").WithValue(jpTitle).WithIsInline(true));

                synonyms.RemoveAll(s => s == "");
                if (synonyms.Count > 0)
                {
                    var synonymsString = "";
                    foreach (var synonym in synonyms) synonymsString += synonym + ", ";
                    synonymsString = synonymsString.TrimEnd(',', ' ');
                    embed.AddField(f => f.WithName("Synonyms").WithValue(synonymsString));
                }

                embed.AddField(f => f.WithName("Type").WithValue(type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(status).WithIsInline(true));

                embed.AddField(f => f.WithName("Avg. Score (max. 100)").WithValue(averageScore == 0 ? "?" : averageScore.ToString()).WithIsInline(true));
                embed.AddField(f => f.WithName("Episodes").WithValue(totalEpisodes == 0 ? "?" : totalEpisodes.ToString()).WithIsInline(true));

                embed.AddField(f => f.WithName("Source Material").WithValue(sourceMaterial ?? "Unknown").WithIsInline(true));
                embed.AddField(f => f.WithName("Duration").WithValue(duration == 0 ? "?" : $"{duration} min.").WithIsInline(true));

                embed.AddField(f => f.WithName("Start Date").WithValue(startDate.ToString("MMMM dd, yyyy")).WithIsInline(true));
                embed.AddField(f => f.WithName("End Date").WithValue(endDate.Year == 1 ? "?" : endDate.ToString("MMMM dd, yyyy")).WithIsInline(true));

                genres.RemoveAll(s => s == "");
                if (genres.Count > 0)
                {
                    var genresString = "";
                    foreach (var genre in genres) genresString += genre + ", ";
                    genresString = genresString.TrimEnd(',', ' ');
                    embed.AddField(f => f.WithName("Genres").WithValue(genresString));
                }

                embed.AddField(f => f.WithName("Description").WithValue(description));
                embed.WithFooter(f => f.WithText("Source: AniList"));
                embed.WithColor(color);
                return embed.Build();
            }
        }

        HttpClient httpClient = new HttpClient { BaseAddress = new Uri("https://anilist.co/api/anime/search/") };
        private string clientId;
        private string clientSecret;
        private string accessToken = "";
        private Color moduleColor = new Color(2, 169, 255);

        async Task ITimedService.Callback()
        {
            var client = new HttpClient();

            var postContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret)
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
            catch(Exception) { }
        }

        async Task<AnimeResult> GetAnime(string query)
        {
            try
            {
                var client = new HttpClient();
                var responseText = await (await client.GetAsync($"{query}?access_token={accessToken}")).Content.ReadAsStringAsync();
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                dynamic firstResult = responseObject[0];

                var result = new AnimeResult
                {
                    id = firstResult["id"],
                    jpTitle = firstResult["title_japanese"],
                    romanizedTitle = firstResult["title_romaji"],
                    englishTitle = firstResult["title_english"]
                };

                if (firstResult["synonyms"].Count > 0)
                {
                    foreach (var synonym in firstResult["synonyms"])
                    {
                        result.synonyms.Add((string)synonym);
                    }
                }

                if (firstResult["genres"].Count > 0)
                {
                    foreach (var genre in firstResult["genres"])
                    {
                        result.genres.Add((string)genre);
                    }
                }

                result.status = firstResult["airing_status"];
                result.type = firstResult["type"];
                result.averageScore = firstResult["average_score"];
                result.totalEpisodes = firstResult["total_episodes"];
                result.duration = firstResult["duration"] ?? 0;
                result.sourceMaterial = firstResult["source"];
                result.description = ((string)firstResult["description"]).Truncate(500);
                result.description = Regex.Replace(result.description, @"\<.+?\>", "");
                result.thumbnailUrl = firstResult["image_url_med"];

                result.startDate = DateTime.Parse((string)firstResult["start_date"]);
                DateTime.TryParse((string)firstResult["end_date"], out result.endDate);

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Embed> GetEmbed(string query)
        {
            return (await GetAnime(query))?.GetEmbed(moduleColor) ??
                   new EmbedBuilder().AddField(f => f.WithName("Error").WithValue("Anime not found"))
                       .WithColor(moduleColor);
        }
    }
}
