using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using Shinoa.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class AnilistModule : Abstract.Module
    {
        public static Color MODULE_COLOR = new Color(2, 169, 255);

        public class AnimeResult
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

            public Embed GetEmbed()
            {
                var embed = new EmbedBuilder();
                embed.Title = romanizedTitle;
                embed.Url = $"http://anilist.co/anime/{id}";
                embed.ThumbnailUrl = thumbnailUrl;

                if (englishTitle != null) embed.AddField(f => f.WithName("English Title").WithValue(englishTitle).WithIsInline(true));
                if (jpTitle != null) embed.AddField(f => f.WithName("Japanese Title").WithValue(jpTitle).WithIsInline(true));

                synonyms.RemoveAll(s => s == "");
                if (synonyms.Count > 0)
                {
                    var synonymsString = "";
                    foreach (var synonym in synonyms) synonymsString += synonym + ", ";
                    synonymsString = synonymsString.TrimEnd(new char[] { ',', ' ' });
                    embed.AddField(f => f.WithName("Synonyms").WithValue(synonymsString));
                }

                embed.AddField(f => f.WithName("Type").WithValue(type).WithIsInline(true));
                embed.AddField(f => f.WithName("Status").WithValue(status).WithIsInline(true));

                embed.AddField(f => f.WithName("Avg. Score (max. 100)").WithValue(averageScore == 0 ? "?" : averageScore.ToString()).WithIsInline(true));
                embed.AddField(f => f.WithName("Episodes").WithValue(totalEpisodes == 0 ? "?" : totalEpisodes.ToString()).WithIsInline(true));

                embed.AddField(f => f.WithName("Source Material").WithValue(sourceMaterial == null ? "Unknown" : sourceMaterial).WithIsInline(true));
                embed.AddField(f => f.WithName("Duration").WithValue(duration == 0 ? "?" : $"{duration} min.").WithIsInline(true));

                embed.AddField(f => f.WithName("Start Date").WithValue(startDate.ToString("MMMM dd, yyyy")).WithIsInline(true));
                embed.AddField(f => f.WithName("End Date").WithValue(endDate.Year == 1 ? "?" : endDate.ToString("MMMM dd, yyyy")).WithIsInline(true));

                genres.RemoveAll(s => s == "");
                if (genres.Count > 0)
                {
                    var genresString = "";
                    foreach (var genre in genres) genresString += genre + ", ";
                    genresString = genresString.TrimEnd(new char[] { ',', ' ' });
                    embed.AddField(f => f.WithName("Genres").WithValue(genresString));
                }

                embed.AddField(f => f.WithName("Description").WithValue(description));
                embed.WithFooter(f => f.WithText("Source: AniList"));
                embed.WithColor(MODULE_COLOR);
                return embed.Build();
            }
        }

        Timer tokenRefreshTimer;
        public static string AccessToken;
        string clientId;
        string clientSecret;

        public override void Init()
        {
            clientId = Shinoa.Config["anilist_client_id"];
            clientSecret = Shinoa.Config["anilist_client_secret"];

            tokenRefreshTimer = new Timer(s =>
            {
                try
                {
                    AccessToken = GetAccessToken();
                }
                catch (Exception e)
                {
                    Logging.Log(e.ToString());
                }
            },
            null,
            TimeSpan.FromSeconds(0),
            TimeSpan.FromMinutes(30));
        }

        string GetAccessToken()
        {
            var client = new HttpClient();

            var postContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", this.clientId),
                    new KeyValuePair<string, string>("client_secret", this.clientSecret),
                });

            var responseString = client.HttpPost("https://anilist.co/api/auth/access_token", postContent);
            dynamic responseObject = JsonConvert.DeserializeObject(responseString);
            return responseObject.access_token;
        }

        public static AnimeResult GetAnime(string query)
        {
            try
            {
                var client = new HttpClient();
                var responseText = client.HttpGet($"https://anilist.co/api/anime/search/{query}?access_token={AccessToken}");
                dynamic responseObject = JsonConvert.DeserializeObject(responseText);

                dynamic firstResult = responseObject[0];

                var result = new AnimeResult();
                result.id = firstResult["id"];
                result.jpTitle = firstResult["title_japanese"];
                result.romanizedTitle = firstResult["title_romaji"];
                result.englishTitle = firstResult["title_english"];

                if (firstResult["synonyms"].Count > 0)
                {
                    foreach (var synonym in firstResult["synonyms"])
                    {
                        result.synonyms.Add((string) synonym);
                    }
                }

                if (firstResult["genres"].Count > 0)
                {
                    foreach (var genre in firstResult["genres"])
                    {
                        result.genres.Add((string) genre);
                    }
                }

                result.status = firstResult["airing_status"];
                result.type = firstResult["type"];
                result.averageScore = firstResult["average_score"];
                result.totalEpisodes = firstResult["total_episodes"];
                result.duration = firstResult["duration"] == null ? 0 : firstResult["duration"];
                result.sourceMaterial = firstResult["source"];
                result.description = ((string)firstResult["description"]).Truncate(500);
                result.description = Regex.Replace(result.description, @"\<.+?\>", "");
                result.thumbnailUrl = firstResult["image_url_med"];

                result.startDate = DateTime.Parse((string) firstResult["start_date"]);
                DateTime.TryParse((string) firstResult["end_date"], out result.endDate);

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        [@Command("anilist", "al", "ani")]
        public async Task AnilistCommand(CommandContext c, params string[] args)
        {
            var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;

            var result = GetAnime(args.ToRemainderString());
            if (result != null)
            {
                await responseMessage.ModifyToEmbedAsync(result.GetEmbed());
            }
            else
            {
                await responseMessage.ModifyAsync(p => p.Content = "Anime not found.");
            }
        }
    }
}
