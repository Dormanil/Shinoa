using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Xml.Linq;
using System.Threading;
using System.Net.Http;
using Discord.Commands;
using System.Text.RegularExpressions;
using Shinoa.Services;
using Shinoa.Services.TimedServices;

namespace Shinoa.Modules
{
    //TODO: Improve migrate
    public class MALModule : ModuleBase<SocketCommandContext>
    {
        private MALService service;
        private AnilistService fallbackService;

        public MALModule(MALService svc, AnilistService fallbackSvc)
        {
            service = svc;
            fallbackService = fallbackSvc;
        }

        [Command("anime"), Alias("mal", "malanime")]
        public async Task MALAnimeSearch([Remainder]string name)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var result = service.GetAnime(name);
            var responseMessage = await responseMessageTask;
            if (result != null)
            {
                await responseMessage.ModifyToEmbedAsync(result.GetEmbed(service.ModuleColor));
            }
            else
            {
                var fallbackResult = await fallbackService.GetEmbed(name);
                await responseMessage.ModifyToEmbedAsync(fallbackResult);
            }
        }

        [Command("manga"), Alias("ln", "malmanga")]
        public async Task MALMangaSearch(string name)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var result = service.GetManga(name);
            var responseMessage = await responseMessageTask;
            if (result != null)
            {
                await responseMessage.ModifyToEmbedAsync(result.GetEmbed(service.ModuleColor));
            }
            else
            {
                await responseMessage.ModifyAsync(p => p.Content = "Manga/LN not found.");
            }
        }
    }
}
