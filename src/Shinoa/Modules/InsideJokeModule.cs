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
        /// Displays information about Catsy.
        /// </summary>
        /// <returns></returns>
        [Command("catsy")]
        public async Task CatsyMessage()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("Catsy:")
                .WithDescription(@"Catsy is the author of the original story of ""Fairy Dance of Death"" (FDD/FDoD).
He is here in an advisory function and it is a great honour for us to have him.
To learn more about FDoD, type `" + Shinoa.Config["global"]["command_prefix"] + "fdod` or `" + Shinoa.Config["global"]["command_prefix"] + "fdd`, whatever suits you more.")
                .WithUrl("https://www.fanfiction.net/u/46508/Catsy");

            await Context.Channel.SendEmbedAsync(embed);
        }

        /// <summary>
        /// Displays information about Fairy Dance of Death.
        /// </summary>
        /// <returns></returns>
        [Command("fdd")]
        [Alias("fdod")]
        public async Task FdoDMessage()
        {
            var embed = new EmbedBuilder();
            embed.WithTitle("Fairy Dance of Death:")
                .WithDescription("Fairy Dance of Death is an Alternate Universe fan-fiction novel of Sword Art Online, exploring the idea of what would have happened if Kayaba Akihiko liked Norse mythology more than a floating castle and created ALO instead of SAO, death game and all that included. It is currently 41 chapters long, still being written, and in its third arc.")
                .WithUrl("https://www.fanfiction.net/s/8679666/1/Fairy-Dance-of-Death");

            await Context.Channel.SendEmbedAsync(embed);
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

        /// <summary>
        /// Tells the current time in Alfheim.
        /// </summary>
        /// <returns></returns>
        [Command("time")]
        public async Task TimeMessage()
        {
            var alfheimTime = GetAlfheimTime();
            var embed = new EmbedBuilder()
                .WithTitle("Current Date and Time in Alfheim")
                .AddField(field => field.WithName("Date:").WithValue($"{alfheimTime.ToString(new CultureInfo("en-US").DateTimeFormat.LongDatePattern)}"))
                .AddField(field => field.WithName("Time:").WithValue($"{alfheimTime.ToString(new CultureInfo("de-DE").DateTimeFormat.LongTimePattern)}"));
            await Context.Channel.SendEmbedAsync(embed);
        }

        private static DateTimeOffset GetAlfheimTime() => new DateTimeOffset(2022, 11, 6, 4, 0, 0, TimeSpan.FromHours(9)).AddTicks(DateTimeOffset.Now.Subtract(new DateTimeOffset(2017, 1, 21, 4, 0, 0, TimeSpan.FromHours(9))).Ticks);
    }
}
