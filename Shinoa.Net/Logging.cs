using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shinoa.Net
{
    class Logging
    {
        public static void Log(string message)
        {
            Console.WriteLine(message);
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
    }
}
