using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Shinoa.Net
{
    class Convenience
    {
        public static string RemoveMentions(string message)
        {
            var mentionRegexPattern = @"<@.*>";
            var mentionRegex = new Regex(mentionRegexPattern);

            return mentionRegex.Replace(message, "");
        }

        public static bool ContainsBotMention(Discord.Message message)
        {
            return message.RawText.Contains($"{ShinoaNet.DiscordClient.CurrentUser.Id}>");
        }
    }
}
