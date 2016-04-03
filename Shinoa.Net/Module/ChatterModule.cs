using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;

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
                        Logging.LogMessage(e.Message);

                        string[] replies = { "Good night~", "Nighty night~", "Oyasumi~" };
                        e.Channel.SendMessage(replies[new Random().Next(replies.Length)]);
                        LastGreetingTime = DateTime.Now;
                    }
                    else if (cleanMessage.Contains("good morning") || cleanMessage.Contains("ohayo") || cleanMessage.Equals("morning"))
                    {
                        Logging.LogMessage(e.Message);

                        string[] replies = { "Morning~", "Ohayou~", "Good morning yourself!" };
                        e.Channel.SendMessage(replies[new Random().Next(replies.Length)]);
                        LastGreetingTime = DateTime.Now;
                    }
                }

                if (e.Message.Text.Trim().Equals(@"o/"))
                {
                    e.Channel.SendMessage(@"\o");
                    Logging.LogMessage(e.Message);
                }
                else if (e.Message.Text.Trim().Equals(@"\o"))
                {
                    e.Channel.SendMessage(@"o/");
                    Logging.LogMessage(e.Message);
                }
                else if (e.Message.Text.Trim().Equals(@"/o/"))
                {
                    e.Channel.SendMessage(@"\o\");
                    Logging.LogMessage(e.Message);
                }
                else if (e.Message.Text.Trim().Equals(@"\o\"))
                {
                    e.Channel.SendMessage(@"/o/");
                    Logging.LogMessage(e.Message);
                }
                else if (new Regex(@"^i'm (.*)$").IsMatch(Convenience.RemoveMentions(e.Message.Text).Trim().ToLower()) &&
                        e.Message.Text.Length <= 20)
                {
                    var regex = new Regex(@"^I'm (.*)$");

                    var thing = regex.Matches(Convenience.RemoveMentions(e.Message.Text).Trim(new char[] { ' ', '.' }))[0].Groups[1];

                    e.Channel.SendMessage($"Hi {thing}, I'm Shinoa.");
                }
                else if (cleanMessage.Equals("soon"))
                {
                    Logging.LogMessage(e.Message);

                    List<object> soonImages = ShinoaNet.Config["soon_images"];
                    e.Channel.SendMessage((string) soonImages[new Random().Next(soonImages.Count)]);
                }
            }
        }

        public string DetailedStats()
        {
            return null;
        }
    }
}
