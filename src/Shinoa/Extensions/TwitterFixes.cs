// <copyright file="TwitterFixes.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using BoxKite.Twitter;
    using BoxKite.Twitter.Extensions;
    using BoxKite.Twitter.Models;

    using Newtonsoft.Json;

    public static class TwitterFixes
    {
        public static async Task<IEnumerable<Tweet>> GetUserTimeline(this ITwitterSession session, string screenName = "", bool extended = true, long userId = 0, long sinceId = 0, long maxId = 0, int count = 200, bool excludeReplies = true, bool includeRetweets = true)
        {
            if (!extended)
                return await session.GetUserTimeline(screenName, userId, sinceId, maxId, count, excludeReplies, includeRetweets);

            var parameters = new TwitterParametersCollection();
            parameters.Create(include_entities: true, include_rts: true, count: count, since_id: sinceId, max_id: maxId, screen_name: screenName);
            parameters.Add("tweet_mode", "extended");

            var responseTask = session.GetAsync(TwitterApi.Resolve("/1.1/statuses/user_timeline.json"), parameters);
            var collection = await responseTask.MapToMany<FullTextTweet>();
            return collection;
        }

        internal static void Create(
            this TwitterParametersCollection parameters,
            bool? include_entities = null,
            long? since_id = null,
            long? max_id = null,
            int? count = null,
            long? user_id = null,
            string screen_name = null,
            long? id = null,
            long? cursor = null,
            string text = null,
            bool? follow = null,
            bool? device = null,
            bool? retweets = null,
            bool? skip_status = null,
            string slug = null,
            long? list_id = null,
            string owner_screen_name = null,
            long? owner_id = null,
            string name = null,
            bool? include_rts = null,
            string place_id = null,
            bool? stall_warnings = null,
            bool? delimited = null,
            bool? full_text = null)
        {
            if (stall_warnings != null)
            {
                parameters.Add("stall_warnings", stall_warnings.ToString());
            }

            if (delimited != null)
            {
                parameters.Add("delimited", delimited.ToString());
            }

            if (cursor != null)
            {
                parameters.Add("cursor", cursor.ToString());
            }

            if (id != null)
            {
                if (id != 0)
                {
                    parameters.Add("id", id.ToString());
                }
            }

            if (list_id != null)
            {
                if (list_id != 0)
                {
                    parameters.Add("list_id", list_id.ToString());
                }
            }

            if (since_id != null)
            {
                if (since_id != 0)
                {
                    parameters.Add("since_id", since_id.ToString());
                }
            }

            if (max_id != null)
            {
                if (max_id != 0)
                {
                    parameters.Add("max_id", max_id.ToString());
                }
            }

            if (count != null)
            {
                if (count != 0)
                {
                    parameters.Add("count", count.ToString());
                }
            }

            if (user_id != null)
            {
                if (user_id != 0)
                {
                    parameters.Add("user_id", user_id.ToString());
                }
            }

            if (owner_id != null)
            {
                if (owner_id != 0)
                {
                    parameters.Add("owner_id", owner_id.ToString());
                }
            }

            if (include_rts != null)
            {
                parameters.Add("include_rts", include_rts.ToString());
            }

            if (include_entities != null)
            {
                parameters.Add("include_entities", include_entities.ToString());
            }

            if (full_text != null)
            {
                parameters.Add("full_text", full_text.ToString());
            }

            if (follow != null)
            {
                parameters.Add("follow", follow.ToString());
            }

            if (skip_status != null)
            {
                parameters.Add("skip_status", skip_status.ToString());
            }

            if (!string.IsNullOrWhiteSpace(screen_name))
            {
                parameters.Add("screen_name", screen_name);
            }

            if (!string.IsNullOrWhiteSpace(place_id))
            {
                parameters.Add("place_id", place_id);
            }

            if (!string.IsNullOrWhiteSpace(owner_screen_name))
            {
                parameters.Add("owner_screen_name", owner_screen_name);
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                parameters.Add("name", name);
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                parameters.Add("slug", slug);
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                parameters.Add("text", text);
            }
        }

        internal static async Task<TwitterResponseCollection<T>> MapToMany<T>(this Task<HttpResponseMessage> task)
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                return new TwitterResponseCollection<T> { twitterFaulted = true, twitterControlMessage = MapHTTPResponses(task) };
            }

            var result = await task;
            if (!result.IsSuccessStatusCode)
            {
                return new TwitterResponseCollection<T> { twitterFaulted = true, twitterControlMessage = MapHTTPResponses(task) };
            }

            var content = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TwitterResponseCollection<T>>(content);
        }

        internal static TwitterControlMessage MapHTTPResponses(this Task<HttpResponseMessage> m)
        {
            var twitterControlMessage = new TwitterControlMessage();
            try
            {
                var bodyContent = m.Result.Content.ReadAsStringAsync();
                /* {StatusCode: 429, ReasonPhrase: 'Too Many Requests', Version: 1.1, Content: System.Net.Http.StreamContent, Headers:*/
                twitterControlMessage.http_status_code = IntValueForHTTPHeaderKey("StatusCode", m.Result.ToString());
                twitterControlMessage.twitter_rate_limit_limit = IntValueForHTTPHeaderKey("x-rate-limit-limit", m.Result.ToString());
                twitterControlMessage.twitter_rate_limit_remaining = IntValueForHTTPHeaderKey("x-rate-limit-remaining", m.Result.ToString());
                twitterControlMessage.twitter_rate_limit_reset = DateTimeForHTTPHeaderKey("x-rate-limit-reset", m.Result.ToString());

                // when posting images, these responses may appear
                twitterControlMessage.twitter_mediaratelimit_limit = IntValueForHTTPHeaderKey("x-mediaratelimit-limit", m.Result.ToString());
                twitterControlMessage.twitter_mediaratelimit_remaining = IntValueForHTTPHeaderKey("x-mediaratelimit-remaining", m.Result.ToString());
                twitterControlMessage.twitter_mediaratelimit_class = StringValueForHTTPHeaderKey("x-mediaratelimit-class", m.Result.ToString());

                if (twitterControlMessage.http_status_code == 429)
                    twitterControlMessage.twitter_error_message = "Rate Limit Exceeded";
                else
                    twitterControlMessage.http_reason = StringValueForHTTPHeaderKey("ReasonPhrase", m.Result.ToString());

                var errordetail = JsonConvert.DeserializeObject<TwitterError>(bodyContent.Result);
                if (errordetail != null)
                {
                    twitterControlMessage.twitter_error_message = errordetail.errors[0].message;
                    twitterControlMessage.twitter_error_code = errordetail.errors[0].code;
                }
            }
            catch (Exception e)
            {
                twitterControlMessage.twitter_error_message = e.Message;
                twitterControlMessage.twitter_error_code = 42;
            }

            return twitterControlMessage;
        }

        internal static int IntValueForHTTPHeaderKey(string key, string header)
        {
            var val = 0;
            var statuskey = new Regex(key + @":\s?([0123456789]+),");
            var mc = statuskey.Matches(header);
            if (mc.Count > 0)
            {
                val = int.Parse(mc[0].Groups[1].Value);
            }

            return val;
        }

        internal static string StringValueForHTTPHeaderKey(string key, string header)
        {
            var val = string.Empty;
            var statuskey = new Regex(key + @":\s?'(.+)'");
            var mc = statuskey.Matches(header);
            if (mc.Count > 0)
            {
                val = mc[0].Groups[1].Value;
            }

            return val;
        }

        internal static DateTime DateTimeForHTTPHeaderKey(string key, string header)
        {
            double val = 0;

            // http://stackoverflow.com/questions/16621738/d-less-efficient-than-0-9
            var statuskey = new Regex(key + @":\s?([0123456789]+)");
            var mc = statuskey.Matches(header);
            if (mc.Count > 0)
            {
                val = double.Parse(mc[0].Groups[1].Value);
            }

            return val.FromUnixEpochSeconds();
        }

        public class FullTextTweet : Tweet
        {
            [JsonProperty("full_text")]
            public new string RawText
            {
                get => base.RawText;

                set => base.RawText = value;
            }
        }
    }
}
