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
                }
            };
        }
    }
}
