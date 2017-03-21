// <copyright file="Util.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.Rest;

    public static class Util
    {
        public static async Task<bool> TrySendEmbedAsync(this IMessageChannel channel, Embed embed)
        {
            try
            {
                var sendMessageTask = channel.SendMessageAsync(string.Empty, embed: embed);
                await sendMessageTask;
                return true;
            }
            catch (Exception e)
            {
                await Logging.LogError(e);
                return false;
            }
        }

        public static Task ModifyToEmbedAsync(this IUserMessage message, Embed embed)
        {
            return message.ModifyAsync(p =>
            {
                p.Content = string.Empty;
                p.Embed = embed;
            });
        }

        public static void SetBasicHttpCredentials(this HttpClient client, string username, string password)
        {
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public static string HttpGet(this HttpClient client, string relativeUrl)
        {
            var response = client.GetAsync(relativeUrl).Result;
            var content = response.Content;
            return response.IsSuccessStatusCode ? content.ReadAsStringAsync().Result : null;
        }

        public static string HttpPost(this HttpClient client, string relativeUrl, HttpContent httpContent)
        {
            var response = client.PostAsync(relativeUrl, httpContent).Result;
            var content = response.Content;
            return response.IsSuccessStatusCode ? content.ReadAsStringAsync().Result : null;
        }

        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }

        public static string FirstParagraph(this string value)
        {
            return value.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)[0];
        }

        public static int ParagraphCount(this string value)
        {
            return value.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        public static IEnumerable<int> To(this int from, int to)
        {
            if (from < to)
            {
                while (from <= to)
                {
                    yield return from++;
                }
            }
            else
            {
                while (from >= to)
                {
                    yield return from--;
                }
            }
        }

        public static bool TryReplyAsync(this ModuleBase<SocketCommandContext> module, string message, out Task<RestUserMessage> userMessage, bool isTTS = false, Embed embed = null, RequestOptions requestOptions = null)
        {
            try
            {
                userMessage = module.Context.Channel.SendMessageAsync(message, isTTS, embed, requestOptions);
                return true;
            }
            catch (Exception e)
            {
                Logging.LogError(e).Wait();
                userMessage = null;
                return false;
            }
        }
    }
}
