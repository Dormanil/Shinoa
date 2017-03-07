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
    public class SauceModule : ModuleBase<SocketCommandContext>
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
                Title = title;
                SimilarityPercentage = similarityPercentage;
                SourceURL = sourceUrl;
                ArtistName = artistName;
                ThumbnailImageURL = thumbmnailImageUrl;
            }

            public Embed GetEmbed()
            {
                var embed = new EmbedBuilder()
                    .WithTitle(SimilarityPercentage > 90 ? Title : $"{Title} (unlikely match)")
                    .AddField(f => f.WithName("Source").WithValue(SourceURL))
                    .AddField(f => f.WithName("Artist Name").WithValue(ArtistName).WithIsInline(true))
                    .AddField(f => f.WithName("Similarity").WithValue($"{SimilarityPercentage}%").WithIsInline(true))
                    .WithFooter(f => f.WithText("Powered by SauceNAO"));

                if (SimilarityPercentage > 90)
                    embed.Color = new Color(139, 195, 74);
                else
                    embed.Color = new Color(96, 125, 139);

                if (!ThumbnailImageURL.Contains("blocked") && !ThumbnailImageURL.Contains(".gif"))
                    embed.ThumbnailUrl = ThumbnailImageURL;

                return embed.Build();
            }
        }

        [Command("sauce"), Alias("source", "saucenao")]
        public async Task SAOWikiaSearch(string url = "")
        {
            var responseMessage = ReplyAsync("Searching...").Result;
            var imageUrl = url == "" ? await FindRelevantImageURLAsync() : url;

            if (imageUrl == null)
            {
                await responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle("Found no suitable image to look up.")
                    .WithColor(new Color(244, 67, 54)));

                return;
            }

            try
            {
                var sauceResult = GetSauce(imageUrl);
                await responseMessage.ModifyToEmbedAsync(sauceResult.GetEmbed());
            }
            catch (SauceNotFoundException)
            {
                await responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle("No match found.")
                    .WithColor(new Color(244, 67, 54)));

                return;
            }
        }

        private async Task<string> FindRelevantImageURLAsync()
        {
            var imageUrl = "";

            if (Context.Message.Attachments.Count > 0)
            {
                imageUrl = Context.Message.Attachments.First().Url;
            }
            else
            {
                var messages = await Context.Channel.GetMessagesAsync(limit: 30).Flatten();
                foreach (var message in messages.OrderByDescending(o => o.Timestamp))
                {
                    if (message.Attachments.Count > 0)
                    {
                        imageUrl = message.Attachments.First().Url;
                        break;
                    }
                    else if (Regex.IsMatch(message.Content, @"http\S*(png|jpg|jpeg)"))
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
