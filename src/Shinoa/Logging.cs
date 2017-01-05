using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa
{
    public class Logging
    {
        static ITextChannel LoggingChannel;

        public static void Log(string message)
        {
            PrintWithTime(message);
            if (LoggingChannel != null) LoggingChannel.SendMessageAsync(message);
        }

        public static void LogMessage(CommandContext context)
        {
            if (!context.IsPrivate)
            {
                Log($"[{context.Guild.Name} -> #{context.Channel.Name}] {context.User.Username}: {context.Message.Content.ToString()}");
            }
            else
            {
                Log($"[PM] {context.User.Username}: {context.Message.Content.ToString()}");
            }
        }

        public static void InitLoggingToChannel()
        {
            var loggingChannelId = Shinoa.Config["logging_channel_id"];
            LoggingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(loggingChannelId));

            LoggingChannel.SendMessageAsync("Logging initialized.");
        }

        private static void PrintWithTime(string line)
        {
            Console.WriteLine($"[{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}] {line}");
        }
    }
}
