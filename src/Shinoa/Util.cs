using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shinoa
{
    public static class Util
    {
        public static void SendPermissionErrorAsync(this IMessageChannel channel, string permissionName)
        {
            channel.SendMessageAsync($"Sorry, but you need the `{permissionName}` permission to do that.");
        }

        public static ulong IdFromMention(string mentionString)
        {
            var idString = mentionString
                .Trim()
                .Replace("<", "")
                .Replace(">", "")
                .Replace("@", "")
                .Replace("!", "");

            return ulong.Parse(idString);
        }

        public static string RemoveMentions(string message)
        {
            var mentionRegexPattern = @"<@.*>";
            var mentionRegex = new Regex(mentionRegexPattern);

            return mentionRegex.Replace(message, "");
        }
    }
}
