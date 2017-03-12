using System;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;

namespace Shinoa.Services
{
    public class FunService : IService
    {
        void IService.Init(dynamic config, IDependencyMap map)
        {
            var client = map.Get <DiscordSocketClient>();
            client.MessageReceived += async (m) =>
            {
                if (m.Author.Id == client.CurrentUser.Id) return;

                switch (m.Content)
                {
                    case @"o/":
                        await m.Channel.SendMessageAsync(@"\o");
                        break;
                    case @"\o":
                        await m.Channel.SendMessageAsync(@"o/");
                        break;
                    case @"/o/":
                        await m.Channel.SendMessageAsync(@"\o\");
                        break;
                    case @"\o\":
                        await m.Channel.SendMessageAsync(@"/o/");
                        break;
                    default:
                        if (m.Content.ToLower() == "wake me up")
                            await m.Channel.SendMessageAsync($"_Wakes {m.Author.Username} up inside._");
                        else if (m.Content.ToLower().StartsWith("wake") &&
                                 m.Content.ToLower().EndsWith("up"))
                        {
                            var messageArray = m.Content.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .Skip(1)
                                .Reverse()
                                .Skip(1)
                                .Reverse();

                            await m.Channel.SendMessageAsync($"_Wakes {messageArray.Aggregate("", (current, word) => current + (word + " ")).Trim()} up inside._");
                        }
                        break;
                }
            };
        }
    }
}
