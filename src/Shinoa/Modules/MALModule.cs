// <copyright file="MALModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Threading.Tasks;
    using Discord.Commands;
    using Services;
    using Services.TimedServices;

    // TODO: Improve migrate
    public class MalModule : ModuleBase<SocketCommandContext>
    {
        private MalService service;
        private AnilistService fallbackService;

        public MalModule(MalService svc, AnilistService fallbackSvc)
        {
            this.service = svc;
            this.fallbackService = fallbackSvc;
        }

        [Command("anime")]
        [Alias("mal", "malanime")]
        public async Task MalAnimeSearch([Remainder]string name)
        {
            var responseMessageTask = this.ReplyAsync("Searching...");

            var result = this.service.GetAnime(name);
            var responseMessage = await responseMessageTask;
            if (result != null)
            {
                await responseMessage.ModifyToEmbedAsync(result.GetEmbed(this.service.ModuleColor));
            }
            else
            {
                var fallbackResult = await this.fallbackService.GetEmbed(name);
                await responseMessage.ModifyToEmbedAsync(fallbackResult);
            }
        }

        [Command("manga")]
        [Alias("ln", "malmanga")]
        public async Task MalMangaSearch([Remainder]string name)
        {
            var responseMessageTask = this.ReplyAsync("Searching...");

            var result = this.service.GetManga(name);
            var responseMessage = await responseMessageTask;
            if (result != null)
            {
                await responseMessage.ModifyToEmbedAsync(result.GetEmbed(this.service.ModuleColor));
            }
            else
            {
                await responseMessage.ModifyAsync(p => p.Content = "Manga/LN not found.");
            }
        }
    }
}
