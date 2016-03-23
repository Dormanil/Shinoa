using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Shinoa.Net.Module
{
    class ChatterModule : IModule
    {
        TimeSpan MinimumGreetingInterval = TimeSpan.FromMinutes(10);
        DateTime LastGreetingTime;
        

        public void Init()
        {
            LastGreetingTime = DateTime.Now.Subtract(MinimumGreetingInterval);
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            var cleanMessage = Convenience.RemoveMentions(e.Message.RawText).Trim().ToLower();

            if (e.Message.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                if (DateTime.Now - LastGreetingTime > MinimumGreetingInterval)
                {
                    if (cleanMessage.Contains("good night") || cleanMessage.Contains("oyasumi") || cleanMessage.Equals("night"))
                    {
                        string[] replies = { "Good night~", "Nighty night~", "Oyasumi~" };
                        e.Channel.SendMessage(replies[new Random().Next(replies.Length)]);
                        LastGreetingTime = DateTime.Now;
                    }
                    else if (cleanMessage.Contains("good morning") || cleanMessage.Contains("ohayo") || cleanMessage.Equals("morning"))
                    {
                        string[] replies = { "Morning~", "Ohayou~", "Good morning yourself!" };
                        e.Channel.SendMessage(replies[new Random().Next(replies.Length)]);
                        LastGreetingTime = DateTime.Now;
                    }
                }
            }
        }
    }
}
