// <copyright file="InsideJokeModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Module containing commands for use on the FDoD server.
    /// </summary>
    [RequireNotBlacklisted]
    public class InsideJokeModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// It is a mystery to everyone...
        /// </summary>
        /// <returns></returns>
        [Command("mystery")]
        public async Task MysteryMessage()
        {
            var deleteAsync = Context.Message.DeleteAsync();
            var embed = new EmbedBuilder
            {
                Url = "https://www.youtube.com/watch?v=fq3abPnEEGE",
            }.WithTitle("It is a mystery to everyone...");
            await Context.Channel.SendEmbedAsync(embed);
            await deleteAsync;
        }

        /// <summary>
        /// Shows a nice picture of a gentleman cactuar.
        /// </summary>
        /// <returns></returns>
        [Command("gentleman")]
        public async Task GentlemanMessage()
        {
            var deleteAsync = Context.Message.DeleteAsync();
            var embed = new EmbedBuilder
            {
                ImageUrl = "http://i.imgur.com/WE7Hf9b.jpg",
            };
            await Context.Channel.SendEmbedAsync(embed);
            await deleteAsync;
        }

        /// <summary>
        /// Tells people that this is not how it works.
        /// </summary>
        /// <returns></returns>
        [Command("howitworks")]
        public async Task HowItWorksMessage()
        {
            var deleteAsync = Context.Message.DeleteAsync();
            await Context.Channel.SendMessageAsync("http://i.imgur.com/oNObxMf.gifv");
            await deleteAsync;
        }

        /// <summary>
        /// Greets users in a unique fashion.
        /// </summary>
        /// <returns></returns>
        [Command("popo")]
        public async Task PopoMessage()
        {
            var deleteAsync = Context.Message.DeleteAsync();
            var embed = new EmbedBuilder
            {
                ImageUrl = "http://25.media.tumblr.com/tumblr_m9dzwbH9t21r3x7i2o1_500.jpg",
            };
            await Context.Channel.SendEmbedAsync(embed);
            await deleteAsync;
        }
    }
}
