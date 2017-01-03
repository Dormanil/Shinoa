using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa
{
    public class Logging
    {
        static Channel LoggingChannel;

        public static void Log(string message)
        {
            PrintWithTime(message);
            if (LoggingChannel != null) LoggingChannel.SendMessage(message);
        }

        public static void LogMessage(Message message)
        {
            if (!message.Channel.IsPrivate)
            {
                Log($"[{message.Server.Name} -> #{message.Channel.Name}] {message.User.Name}: {message.Text}");
            }
            else
            {
                Log($"[PM] {message.User.Name}: {message.Text}");
            }
        }

        public static void InitLoggingToChannel()
        {
            var loggingChannelId = Shinoa.Config["logging_channel_id"];
            LoggingChannel = Shinoa.DiscordClient.GetChannel(ulong.Parse(loggingChannelId));

            LoggingChannel.SendMessage("Logging initialized.");
        }

        private static void PrintWithTime(string line)
        {
            Console.WriteLine($"[{DateTime.Now.Hour:D2}:{DateTime.Now.Minute:D2}:{DateTime.Now.Second:D2}] {line}");
        }
    }
}
