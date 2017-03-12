using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Shinoa.Attributes;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class InsideJokeModule : Abstract.Module
    {
        [@Command("mystery")]
        public async Task MysteryMessage(CommandContext c, params string[] args)
        {
            await c.Message.DeleteAsync();
            var embed = new EmbedBuilder()
            {
                Url = "https://www.youtube.com/watch?v=fq3abPnEEGE"
            }.WithTitle("It is a mystery to everyone...");
            await c.Channel.SendEmbedAsync(embed);
        }

        [@Command("gentleman")]
        public async Task GentlemanMessage(CommandContext c, params string[] args)
        {
            await c.Message.DeleteAsync();
            var embed = new EmbedBuilder()
            {
                ImageUrl = "http://i.imgur.com/WE7Hf9b.jpg"
            };
            await c.Channel.SendEmbedAsync(embed);
        }

        [@Command("catsy")]
        public async Task CatsyMessage(CommandContext c, params string[] args)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Catsy:")
                .WithDescription(@"Catsy is the author of the original story of ""Fairy Dance of Death"" (FDD/FDoD).
He is here in an advisory function and it is a great honour for us to have him.
To learn more about FDoD, type `" + Shinoa.Config["command_prefix"] + "fdod` or `" + Shinoa.Config["command_prefix"] + "fdd`, whatever suits you more.")
                .WithUrl("https://www.fanfiction.net/u/46508/Catsy");

            await c.Channel.SendEmbedAsync(embed);
        }

        [@Command("fdd", "fdod")]
        public async Task FDoDMessage(CommandContext c, params string[] args)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Fairy Dance of Death:")
                .WithDescription("Fairy Dance of Death is an Alternate Universe fan-fiction novel of Sword Art Online, exploring the idea of what would have happened if Kayaba Akihiko liked Norse mythology more than a floating castle and created ALO instead of SAO, death game and all that included. It is currently 41 chapters long, still being written, and in its third arc.")
                .WithUrl("https://www.fanfiction.net/s/8679666/1/Fairy-Dance-of-Death");

            await c.Channel.SendEmbedAsync(embed);
        }

        [@Command("howitworks")]
        public async Task HowItWorksMessage(CommandContext c, params string[] args)
        {
            await c.Message.DeleteAsync();
            await c.Channel.SendMessageAsync("http://i.imgur.com/oNObxMf.gifv");
        }

        [@Command("popo")]
        public async Task PopoMessage(CommandContext c, params string[] args)
        {
            await c.Message.DeleteAsync();
            var embed = new EmbedBuilder()
            {
                ImageUrl = "http://25.media.tumblr.com/tumblr_m9dzwbH9t21r3x7i2o1_500.jpg"
            };
            await c.Channel.SendEmbedAsync(embed);
        }

        [@Command("time")]
        public async Task TimeMessage(CommandContext c, params string[] args)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Current Date and Time in Alfheim")
                .AddField(field => field.WithName("Date:").WithValue($"{GetAlfheimTime().ToString(new System.Globalization.CultureInfo("en-US").DateTimeFormat.LongDatePattern)}"))
                .AddField(field => field.WithName("Time:").WithValue($"{GetAlfheimTime().ToString(new System.Globalization.CultureInfo("de-DE").DateTimeFormat.LongTimePattern)}"));
            await c.Channel.SendEmbedAsync(embed);
        }

        private static DateTimeOffset GetAlfheimTime() => new DateTimeOffset(2022, 11, 6, 4, 0, 0, TimeSpan.FromHours(9)).AddTicks(DateTimeOffset.Now.Subtract(new DateTimeOffset(2017, 1, 21, 4, 0, 0, TimeSpan.FromHours(9))).Ticks);
    }
}
