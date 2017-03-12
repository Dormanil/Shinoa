using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        public static Color MODULE_COLOR = new Color(63, 81, 181);

        //TODO: Migrate
        public void HandleMessage()
        {
            if (Context.User.Id == Shinoa.Client.CurrentUser.Id) return;
            switch (Context.Message.Content)
            {
                case @"o/":
                    Context.Channel.SendMessageAsync(@"\o");
                    break;
                case @"\o":
                    Context.Channel.SendMessageAsync(@"o/");
                    break;
                case @"/o/":
                    Context.Channel.SendMessageAsync(@"\o\");
                    break;
                case @"\o\":
                    Context.Channel.SendMessageAsync(@"/o/");
                    break;
                default:
                    if (Context.Message.Content.ToLower() == "wake me up")
                        Context.Channel.SendMessageAsync($"_Wakes {Context.User.Username} up inside._");
                    else if (Context.Message.Content.ToLower().StartsWith("wake") &&
                             Context.Message.Content.ToLower().EndsWith("up"))
                    {
                        var messageArray = Context.Message.Content.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)
                            .Skip(1)
                            .Reverse()
                            .Skip(1)
                            .Reverse();

                        Context.Channel.SendMessageAsync($"_Wakes {messageArray.Aggregate("", (current, word) => current + (word + " ")).Trim()} up inside._");
                    }
                    break;
            }
        }

        [Command("pick"), Alias("choose")]
        public async Task Pick([Remainder]string args)
        {
            var choices = args.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
            var choice = choices[new Random().Next(choices.Length)].Trim();

            var embed = new EmbedBuilder()
            .WithTitle($"I choose '{choice}'.")
            .WithColor(MODULE_COLOR);

            await ReplyAsync("", embed: embed.Build());
        }

        [Command("roll"), Alias("rolldice")]
        public async Task RollDice(string arg)
        {
            var rng = new Random();
            var multiplier = int.Parse(arg.Split('d')[0]);
            var dieSize = int.Parse(arg.Split('d')[1]);
            var total = 0;

            if (multiplier > 100)
            {
                await ReplyAsync("Please stick to reasonable amounts of dice.");
                return;
            }

            var rollsString = "";
            foreach (var i in 1.To(multiplier))
            {
                var roll = rng.Next(dieSize) + 1;
                rollsString += $"{roll}, ";
                total += roll;
            }

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Total").WithValue(total.ToString()))
                .AddField(f => f.WithName("Rolls").WithValue(rollsString.Trim(' ', ',')))
                .WithColor(MODULE_COLOR);

           await ReplyAsync("", embed: embed);
        }

        [Command("lenny")]
        public async Task LennyFace()
        {
            var deleteAsync = Context.Message.DeleteAsync();
            await ReplyAsync("( ͡° ͜ʖ ͡°)");
            await deleteAsync;
        }
    }
}
