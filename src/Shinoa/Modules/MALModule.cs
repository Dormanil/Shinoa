// <copyright file="MALModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Threading.Tasks;
    using Attributes;
    using Discord.Commands;
    using Services;
    using Services.TimedServices;

    // TODO: Improve migrate

    /// <summary>
    /// Module for MyAnimeList services.
    /// </summary>
    [RequireNotBlacklisted]
    public class MalModule : ModuleBase<SocketCommandContext>
    {
        private readonly MalService service;
        private readonly AnilistService fallbackService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MalModule"/> class.
        /// </summary>
        /// <param name="svc">Backing service instance.</param>
        /// <param name="fallbackSvc">Backing fallback service instance.</param>
        public MalModule(MalService svc, AnilistService fallbackSvc)
        {
            service = svc;
            fallbackService = fallbackSvc;
        }

        /// <summary>
        /// Command for searching a specific anime using MyAnimeList.
        /// </summary>
        /// <param name="name">Name of the anime.</param>
        /// <returns></returns>
        [Command("anime")]
        [Alias("mal", "malanime")]
        public async Task MalAnimeSearch([Remainder]string name)
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

        /// <summary>
        /// Command for searching for a specific Manga using MyAnimeList.
        /// </summary>
        /// <param name="name">Name of the manga.</param>
        /// <returns></returns>
        [Command("manga")]
        [Alias("ln", "malmanga")]
        public async Task MalMangaSearch([Remainder]string name)
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
