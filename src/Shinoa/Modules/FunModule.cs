// <copyright file="FunModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;

    public class FunModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Color ModuleColor = new Color(63, 81, 181);

        [Command("pick")]
        [Alias("choose")]
        public async Task Pick([Remainder]string args)
        {
            var choices = args.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
            var choice = choices[new Random().Next(choices.Length)].Trim();

            var embed = new EmbedBuilder()
            .WithTitle($"I choose '{choice}'.")
            .WithColor(ModuleColor);

            await Context.Channel.SendEmbedAsync(embed);
        }

        [Command("roll")]
        [Alias("rolldice")]
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

            var rollsString = string.Empty;
            foreach (var i in 1.To(multiplier))
            {
                var roll = rng.Next(dieSize) + 1;
                rollsString += $"{roll}, ";
                total += roll;
            }

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Total").WithValue(total.ToString()))
                .AddField(f => f.WithName("Rolls").WithValue(rollsString.Trim(' ', ',')))
                .WithColor(ModuleColor);
            await Context.Channel.SendEmbedAsync(embed);
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
