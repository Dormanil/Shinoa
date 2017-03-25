// <copyright file="AnilistModule.cs" company="The Shinoa Development Team">
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
    using Services.TimedServices;

    /// <summary>
    /// Module for Anilist services.
    /// </summary>
    [RequireNotBlacklisted]
    public class AnilistModule : ModuleBase<SocketCommandContext>
    {
        private readonly AnilistService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnilistModule"/> class.
        /// </summary>
        /// <param name="svc">Backing service instance.</param>
        public AnilistModule(AnilistService svc)
        {
            service = svc;
        }

        /// <summary>
        /// Command to search for an anime using Anilist.
        /// </summary>
        /// <param name="name">Name of the anime to search for.</param>
        /// <returns></returns>
        [Command("anilist")]
        [Alias("al", "ani")]
        public async Task AnilistCommand([Remainder]string name)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var result = await service.GetEmbed(name);
            var responseMessage = await responseMessageTask;
            await responseMessage.ModifyToEmbedAsync(result);
        }
    }
}
