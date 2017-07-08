// <copyright file="Util.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    using Discord;

    public static class Util
    {
        public static Task<IUserMessage> SendEmbedAsync(this IMessageChannel channel, Embed embed)
        {
            return channel.SendMessageAsync(string.Empty, embed: embed);
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

        public static async Task<string> HttpGet(this HttpClient client, string relativeUrl)
        {
            var response = await client.GetAsync(relativeUrl);
            var content = response.Content;
            return response.IsSuccessStatusCode ? await content.ReadAsStringAsync() : null;
        }

        public static async Task<string> HttpPost(this HttpClient client, string relativeUrl, HttpContent httpContent)
        {
            var response = await client.PostAsync(relativeUrl, httpContent);
            var content = response.Content;
            return response.IsSuccessStatusCode ? await content.ReadAsStringAsync() : null;
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

        public static IEnumerable<T> InStepsOf<T>(this IEnumerable<T> source, int step)
        {
            if (step == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(step), "Parameter cannot be zero.");
            }

            return source.Where((x, i) => i % step == 0);
        }

        public static KeyValuePair<T1, T2> ToKeyValuePair<T1, T2>(this(T1, T2) tuple)
        {
            return new KeyValuePair<T1, T2>(tuple.Item1, tuple.Item2);
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var item in enumerable)
            {
                action(item);
                yield return item;
            }
        }
    }
}
