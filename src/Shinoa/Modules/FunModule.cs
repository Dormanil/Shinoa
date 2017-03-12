using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Shinoa.Attributes;

namespace Shinoa.Modules
{
    public class FunModule : Abstract.Module
    {
        public static Color MODULE_COLOR = new Color(63, 81, 181);

        public override void HandleMessage(CommandContext context)
        {
            if (context.User.Id == Shinoa.DiscordClient.CurrentUser.Id) return;
            switch (context.Message.Content)
            {
                case @"o/":
                    context.Channel.SendMessageAsync(@"\o");
                    break;
                case @"\o":
                    context.Channel.SendMessageAsync(@"o/");
                    break;
                case @"/o/":
                    context.Channel.SendMessageAsync(@"\o\");
                    break;
                case @"\o\":
                    context.Channel.SendMessageAsync(@"/o/");
                    break;
                default:
                    if (context.Message.Content.ToLower() == "wake me up")
                        context.Channel.SendMessageAsync($"_Wakes {context.User.Username} up inside._");
                    else if (context.Message.Content.ToLower().StartsWith("wake") &&
                             context.Message.Content.ToLower().EndsWith("up"))
                    {
                        var messageArray = context.Message.Content.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)
                            .Skip(1)
                            .Reverse()
                            .Skip(1)
                            .Reverse();

                        context.Channel.SendMessageAsync($"_Wakes {messageArray.Aggregate("", (current, word) => current + (word + " ")).Trim()} up inside._");
                    }
                    break;
            }
        }

        [@Command("pick", "choose")]
        public void Pick(CommandContext c, params string[] args)
        {
            var choices = args.ToRemainderString().Split(new string[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
            var choice = choices[new Random().Next(choices.Length)].Trim();

            var embed = new EmbedBuilder()
            .WithTitle($"I choose '{choice}'.")
            .WithColor(MODULE_COLOR);

            c.Channel.SendEmbedAsync(embed.Build());
        }

        [@Command("roll", "rolldice")]
        public void RollDice(CommandContext c, params string[] args)
        {
            var rng = new Random();
            var multiplier = int.Parse(args[0].Split('d')[0]);
            var dieSize = int.Parse(args[0].Split('d')[1]);
            var total = 0;

            if (multiplier > 100)
            {
                c.Channel.SendMessageAsync("Please stick to reasonable amounts of dice.");
                return;
            }

            var rollsString = "";
            foreach (int i in 1.To(multiplier))
            {
                int roll = rng.Next(dieSize) + 1;
                rollsString += $"{roll}, ";
                total += roll;
            }

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Total").WithValue(total.ToString()))
                .AddField(f => f.WithName("Rolls").WithValue(rollsString.Trim(' ', ',')))
                .WithColor(MODULE_COLOR);

            c.Channel.SendEmbedAsync(embed);
        }

        [@Command("lenny")]
        public void LennyFace(CommandContext c, params string[] args)
        {
            c.Message.DeleteAsync();
            c.Channel.SendMessageAsync("( ͡° ͜ʖ ͡°)");
        }
    }
}
