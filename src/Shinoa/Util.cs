using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa
{
    public static class Util
    {
        public static void SendPermissionError(this Channel channel, string permissionName)
        {
            channel.SendMessage($"Sorry, but you need the `{permissionName}` permission to do that.");
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
    }
}
