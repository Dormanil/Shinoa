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
        public void MysteryMessage(CommandContext c, params string[] args)
        {
            var embed = new EmbedBuilder()
            {
                Url = "https://www.youtube.com/watch?v=fq3abPnEEGE"
            }.WithTitle("It is a mystery to everyone...");
            c.Channel.SendEmbedAsync(embed);
        }

        [@Command("gentleman")]
        public void GentlemanMessage(CommandContext c, params string[] args)
        {
            var embed = new EmbedBuilder()
            {
                ImageUrl = "http://i.imgur.com/WE7Hf9b.jpg"
            };
            c.Channel.SendEmbedAsync(embed);
        }

        [@Command("catsy")]
        public void CatsyMessage(CommandContext c, params string[] args)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Catsy:")
                .WithDescription(@"Catsy is the author of the original story of ""Fairy Dance of Death"" (FDD/FDoD).
He is here in an advisory function and it is a great honour for us to have him.
To learn more about FDoD, type `" + Shinoa.Config["command_prefix"] + "fdod` or `" + Shinoa.Config["command_prefix"] + "fdd`, whatever suits you more.")
                .WithUrl("https://www.fanfiction.net/u/46508/Catsy");

            c.Channel.SendEmbedAsync(embed);
        }

        [@Command("fdd", "fdod")]
        public void FDoDMessage(CommandContext c, params string[] args)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle("Fairy Dance of Death:")
                .WithDescription("Fairy Dance of Death is an Alternate Universe fan-fiction novel of Sword Art Online, exploring the idea of what would have happened if Kayaba Akihiko liked Norse mythology more than a floating castle and created ALO instead of SAO, death game and all that included. It is currently 41 chapters long, still being written, and in its third arc.")
                .WithUrl("https://www.fanfiction.net/s/8679666/1/Fairy-Dance-of-Death");

            c.Channel.SendEmbedAsync(embed);
        }

        [@Command("howitworks")]
        public void HowItWorksMessage(CommandContext c, params string[] args)
        {
            c.Channel.SendMessageAsync("http://i.imgur.com/oNObxMf.gifv");
        }

        [@Command("popo")]
        public void PopoMessage(CommandContext c, params string[] args)
        {
            var embed = new EmbedBuilder()
            {
                ImageUrl = "http://25.media.tumblr.com/tumblr_m9dzwbH9t21r3x7i2o1_500.jpg"
            };
            c.Channel.SendEmbedAsync(embed);
        }

        [@Command("time")]
        public void TimeMessage(CommandContext c, params string[] args)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Current Date and Time in Alfheim")
                .AddField(field => field.WithName("Date:").WithValue($"{GetAlfheimTime().ToString(new System.Globalization.CultureInfo("en-US").DateTimeFormat.LongDatePattern)}"))
                .AddField(field => field.WithName("Time:").WithValue($"{GetAlfheimTime().ToString(new System.Globalization.CultureInfo("de-DE").DateTimeFormat.LongTimePattern)}"));
            c.Channel.SendEmbedAsync(embed);
        }

        private static DateTimeOffset GetAlfheimTime() => new DateTimeOffset(2022, 11, 6, 4, 0, 0, TimeSpan.FromHours(9)).AddTicks(DateTimeOffset.Now.Subtract(new DateTimeOffset(2017, 1, 21, 4, 0, 0, TimeSpan.FromHours(9))).Ticks);
    }
}
