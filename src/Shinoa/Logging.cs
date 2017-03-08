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
            LoggingChannel?.SendMessageAsync(message).GetAwaiter().GetResult();
        }

        public static void LogMessage(ICommandContext context)
        {
            if (!(context.Channel is IPrivateChannel))
            {
                Log($"[{context.Guild.Name} -> #{context.Channel.Name}] {context.User.Username}: {context.Message.Content}");
            }
            else
            {
                Log($"[PM] {context.User.Username}: {context.Message.Content}");
            }
        }

        public static void InitLoggingToChannel()
        {
            var loggingChannelId = Shinoa.Config["global"]["logging_channel_id"];
            LoggingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(loggingChannelId));

            LoggingChannel.SendMessageAsync("Logging initialized.");
        }

        private static void PrintWithTime(string line)
        {
            Console.WriteLine($"[{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}] {line}");
        }
    }
}
