// <copyright file="MALModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
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
        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public MalService Service { get; set; }

        /// <summary>
        /// Gets or sets the backing fallback service instance.
        /// </summary>
        public AnilistService FallbackService { get; set; }

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

            var result = Service.GetAnime(name);
            var responseMessage = await responseMessageTask;
            if (result != null)
            {
                await responseMessage.ModifyToEmbedAsync(result.GetEmbed(Service.ModuleColor));
            }
            else
            {
                var fallbackResult = await FallbackService.GetEmbed(name);
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

            var result = Service.GetManga(name);
            var responseMessage = await responseMessageTask;
            if (result != null)
            {
                await responseMessage.ModifyToEmbedAsync(result.GetEmbed(Service.ModuleColor));
            }
            else
            {
                await responseMessage.ModifyAsync(p => p.Content = "Manga/LN not found.");
            }
        }
    }
}
