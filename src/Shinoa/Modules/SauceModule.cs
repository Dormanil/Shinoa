using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class SauceModule: Abstract.Module
    {
        static HttpClient httpClient = new HttpClient();

        public class SauceNotFoundException : Exception
        {
        }

        public class SauceResult
        {
            public string Title { get; }
            public float SimilarityPercentage { get; }
            public string SourceURL { get; }
            public string ArtistName { get; }
            public string ThumbnailImageURL { get; }

            public SauceResult(string title, float similarityPercentage, string sourceUrl, string artistName, string thumbmnailImageUrl)
            {
                this.Title = title;
                this.SimilarityPercentage = similarityPercentage;
                this.SourceURL = sourceUrl;
                this.ArtistName = artistName;
                this.ThumbnailImageURL = thumbmnailImageUrl;
            }

            public Embed GetEmbed()
            {
                var embed = new EmbedBuilder()
                    .WithTitle(this.SimilarityPercentage > 90 ? this.Title : $"{this.Title} (unlikely match)")
                    .AddField(f => f.WithName("Source").WithValue(this.SourceURL))
                    .AddField(f => f.WithName("Artist Name").WithValue(this.ArtistName).WithIsInline(true))
                    .AddField(f => f.WithName("Similarity").WithValue($"{this.SimilarityPercentage}%").WithIsInline(true))
                    .WithFooter(f => f.WithText("Powered by SauceNAO"));

                if (this.SimilarityPercentage > 90)
                    embed.Color = new Color(139, 195, 74);
                else
                    embed.Color = new Color(96, 125, 139);

                if (!this.ThumbnailImageURL.Contains("blocked") && !this.ThumbnailImageURL.Contains(".gif"))
                    embed.ThumbnailUrl = this.ThumbnailImageURL;

                return embed.Build();
            }
        }

        public override void Init()
        {
            this.BoundCommands.Add("sauce", (c) =>
            {
                var responseMessage = c.Channel.SendMessageAsync("Searching...").Result;
                var imageUrl = FindRelevantImageURL(c);

                if (imageUrl == null)
                {
                    responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                        .WithTitle("Found no suitable image to look up.")
                        .WithColor(new Color(244, 67, 54)));

                    return;
                }
                
                try
                {
                    var sauceResult = GetSauce(imageUrl);
                    responseMessage.ModifyToEmbedAsync(sauceResult.GetEmbed());
                }
                catch (SauceNotFoundException)
                {
                    responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                        .WithTitle("No match found.")
                        .WithColor(new Color(244, 67, 54)));

                    return;
                }
            });
        }

        private static string FindRelevantImageURL(CommandContext c)
        {
            var imageUrl = "";

            if (c.Message.Content.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries).Length == 1)
            {
                if (c.Message.Attachments.Count > 0)
                {
                    imageUrl = c.Message.Attachments.First().Url;
                }
                else
                {
                    c.Channel.GetMessagesAsync(limit: 30).ForEach(mlist =>
                    {
                        foreach (var message in mlist.ToList().OrderByDescending(o => o.Timestamp))
                        {
                            if (message.Attachments.Count > 0)
                            {
                                imageUrl = message.Attachments.First().Url;
                                break;
                            }
                            else if (message.Content.Contains(".jpg") ||
                                     message.Content.Contains(".jpeg") ||
                                     message.Content.Contains(".png"))
                            {
                                var match = Regex.Match(message.Content, @"http\S*(png|jpg|jpeg)");
                                if (match.Success)
                                {
                                    imageUrl = match.Captures[0].Value;
                                    break;
                                }
                            }
                            else if (message.Embeds.Count > 0)
                            {
                                var firstEmbed = message.Embeds.First();
                                

                                foreach (var field in firstEmbed.Fields)
                                {
                                    if (field.Value.EndsWith(".jpg") || field.Value.EndsWith(".png"))
                                    {
                                        imageUrl = field.Value;
                                        break;
                                    }
                                }

                                if (firstEmbed.Image.HasValue)
                                {
                                    imageUrl = firstEmbed.Image.Value.Url;
                                    break;
                                }
                            }

                            if (imageUrl != "") break;
                        }
                    }); ;
                }
            }
            else
            {
                imageUrl = GetCommandParameters(c.Message.Content)[0];
            }

            return imageUrl == "" ? null : imageUrl;
        }

        public static SauceResult GetSauce(string imageUrl)
        {
            var postContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("urlify", "on"),
                    new KeyValuePair<string, string>("url", imageUrl),
                    new KeyValuePair<string, string>("frame", "1"),
                    new KeyValuePair<string, string>("hide", "0"),
                    new KeyValuePair<string, string>("database", "5")
                });

            var resultPageHtml = httpClient.HttpPost("https://saucenao.com/search.php", postContent);
            
            try
            {
                var document = new HtmlDocument();
                document.LoadHtml(resultPageHtml);

                return new SauceResult(
                    title: document.DocumentNode.SelectNodes(@"//div[@class='resulttitle']/strong")[0].InnerHtml,
                    similarityPercentage: float.Parse(document.DocumentNode.SelectNodes(@"//div[@class='resultsimilarityinfo']")[0].InnerHtml.Replace("%", "")),
                    sourceUrl: document.DocumentNode.SelectNodes(@"//div[@class='resultcontentcolumn']/a")[0].Attributes["href"].Value,
                    artistName: document.DocumentNode.SelectNodes(@"//div[@class='resultcontentcolumn']/a")[1].InnerHtml,
                    thumbmnailImageUrl: document.DocumentNode.SelectNodes(@"//div[@class='resultimage']/a/img")[0].Attributes["src"].Value);
            }
            catch (Exception)
            {
                throw new SauceNotFoundException();
            }
        }
    }
}
