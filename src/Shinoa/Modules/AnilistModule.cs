// <copyright file="AnilistModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
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
        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public AnilistService Service { get; set; }

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

            var result = await Service.GetEmbed(name);
            var responseMessage = await responseMessageTask;
            await responseMessage.ModifyToEmbedAsync(result);
        }
    }
}
