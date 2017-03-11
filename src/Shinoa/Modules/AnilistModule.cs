using System.Threading.Tasks;
using Discord.Commands;
using Shinoa.Services.TimedServices;

namespace Shinoa.Modules
{
    public class AnilistModule : ModuleBase<SocketCommandContext>
    {
        private readonly AnilistService service;

        public AnilistModule(AnilistService svc)
        {
            service = svc;
        }

        [Command("anilist"), Alias("al", "ani")]
        public async Task AnilistCommand([Remainder]string name)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var result = await service.GetEmbed(name);
            var responseMessage = await responseMessageTask;
            await responseMessage.ModifyToEmbedAsync(result);
        }
    }
}
