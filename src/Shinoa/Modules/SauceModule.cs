// <copyright file="SauceModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;

    using Extensions;

    using HtmlAgilityPack;

    /// <summary>
    /// Module to find the source of an image using SauceNAO.
    /// </summary>
    [RequireNotBlacklisted]
    public class SauceModule : ModuleBase<SocketCommandContext>
    {
        private static readonly HttpClient HttpClient = new HttpClient { BaseAddress = new Uri("https://saucenao.com/") };

        /// <summary>
        /// Gets the sauce for a specified image.
        /// </summary>
        /// <param name="imageUrl">URL to the image.</param>
        /// <returns>The <see cref="SauceResult" /> containing the source information of the image.</returns>
        public static SauceResult GetSauce(string imageUrl)
        {
            var postContent = new FormUrlEncodedContent(new[]
                {
                    ("urlify", "on").ToKeyValuePair(),
                    ("url", imageUrl).ToKeyValuePair(),
                    ("frame", "1").ToKeyValuePair(),
                    ("hide", "0").ToKeyValuePair(),
                    ("database", "5").ToKeyValuePair(),
                });

            var resultPageHtml = HttpClient.HttpPost("search.php", postContent).Result;
            if (resultPageHtml == null) throw new SauceNotFoundException();

            try
            {
                var document = new HtmlDocument();
                document.LoadHtml(resultPageHtml);

                return new SauceResult(
                    title: document.DocumentNode.SelectNodes(@"//div[@class='resulttitle']/strong")[0].InnerHtml,
                    similarityPercentage: float.Parse(document.DocumentNode.SelectNodes(@"//div[@class='resultsimilarityinfo']")[0].InnerHtml.Replace("%", string.Empty)),
                    sourceUrl: document.DocumentNode.SelectNodes(@"//div[@class='resultcontentcolumn']/a")[0].Attributes["href"].Value,
                    artistName: document.DocumentNode.SelectNodes(@"//div[@class='resultcontentcolumn']/a")[1].InnerHtml,
                    thumbmnailImageUrl: document.DocumentNode.SelectNodes(@"//div[@class='resultimage']/a/img")[0].Attributes["src"].Value);
            }
            catch (Exception e)
            {
                throw new SauceNotFoundException(string.Empty, e);
            }
        }

        /// <summary>
        /// Searches for the source of an image.
        /// </summary>
        /// <param name="url">Optional: URL for the image to search the sauce for.</param>
        /// <returns></returns>
        [Command("sauce")]
        [Alias("source", "saucenao")]
        public async Task SauceSearch(string url = "")
        {
            var responseMessage = ReplyAsync("Searching...").Result;
            var imageUrl = url == string.Empty ? await FindRelevantImageUrlAsync() : url;

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
                await responseMessage.ModifyToEmbedAsync(sauceResult.Embed);
            }
            catch (SauceNotFoundException)
            {
                await responseMessage.ModifyToEmbedAsync(new EmbedBuilder()
                    .WithTitle("No match found.")
                    .WithColor(new Color(244, 67, 54)));
            }
        }

        private async Task<string> FindRelevantImageUrlAsync()
        {
            var imageUrl = string.Empty;

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

                    if (Regex.IsMatch(message.Content, @"http\S*(png|jpg|jpeg)"))
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
                            if (!field.Value.EndsWith(".jpg") && !field.Value.EndsWith(".png")) continue;
                            imageUrl = field.Value;
                            break;
                        }

                        if (firstEmbed.Image.HasValue)
                        {
                            imageUrl = firstEmbed.Image.Value.Url;
                            break;
                        }
                    }

                    if (imageUrl != string.Empty) break;
                }
            }

            return imageUrl == string.Empty ? null : imageUrl;
        }

        /// <summary>
        /// Exception thrown when the Sauce wasn't found.
        /// </summary>
        public class SauceNotFoundException : Exception
        {
            /// <inheritdoc cref="Exception"/>
            public SauceNotFoundException()
                : base()
            {
            }

            /// <inheritdoc cref="Exception"/>
            public SauceNotFoundException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        /// <summary>
        /// The result of a search for the sauce.
        /// </summary>
        public class SauceResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SauceResult"/> class.
            /// </summary>
            /// <param name="title">Title of the image.</param>
            /// <param name="similarityPercentage">Similarity to the image searched for.</param>
            /// <param name="sourceUrl">URL of the source image.</param>
            /// <param name="artistName">Name of the artist.</param>
            /// <param name="thumbmnailImageUrl">URL to the image used as the thumbnail.</param>
            public SauceResult(string title, float similarityPercentage, string sourceUrl, string artistName, string thumbmnailImageUrl)
            {
                Title = WebUtility.HtmlDecode(title);
                SimilarityPercentage = similarityPercentage;
                SourceUrl = sourceUrl;
                ArtistName = artistName;
                ThumbnailImageUrl = thumbmnailImageUrl;
            }

            /// <summary>
            /// Gets the title of the image.
            /// </summary>
            public string Title { get; }

            /// <summary>
            /// Gets the percentage of similarity to the original image.
            /// </summary>
            public float SimilarityPercentage { get; }

            /// <summary>
            /// Gets the URL of the source image.
            /// </summary>
            public string SourceUrl { get; }

            /// <summary>
            /// Gets the name of the artist.
            /// </summary>
            public string ArtistName { get; }

            /// <summary>
            /// Gets the thumbnail-image URL.
            /// </summary>
            public string ThumbnailImageUrl { get; }

            /// <summary>
            /// Gets the <see cref="Discord.Embed"/> containing the information of the <see cref="SauceResult"/>.
            /// </summary>
            public Embed Embed
            {
                get
                {
                    var embed = new EmbedBuilder()
                        .WithTitle(SimilarityPercentage > 90 ? Title : $"{Title} (unlikely match)")
                        .AddField(f => f.WithName("Source").WithValue(SourceUrl))
                        .AddField(f => f.WithName("Artist Name").WithValue(ArtistName).WithIsInline(true))
                        .AddField(f => f.WithName("Similarity").WithValue($"{SimilarityPercentage}%")
                            .WithIsInline(true))
                        .WithFooter(f => f.WithText("Powered by SauceNAO"));

                    embed.Color = SimilarityPercentage > 90 ? new Color(139, 195, 74) : new Color(96, 125, 139);

                    if (!ThumbnailImageUrl.Contains("blocked") && !ThumbnailImageUrl.Contains(".gif"))
                        embed.ThumbnailUrl = ThumbnailImageUrl;

                    return embed;
                }
            }
        }
    }
}
