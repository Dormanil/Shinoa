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
    using Services;

    /// <summary>
    /// Module for funny commands.
    /// </summary>
    public class FunModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Color ModuleColor = new Color(63, 81, 181);

        /// <summary>
        /// Initializes a new instance of the <see cref="FunModule"/> class.
        /// </summary>
        /// <param name="svc">Backing service instance.</param>
        public FunModule(FunService svc)
        {
            Service = svc;
        }

        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public static FunService Service { get; set; }

        /// <summary>
        /// Command to pick arbitrarily between a number of choices.
        /// </summary>
        /// <param name="args">Option string</param>
        /// <returns></returns>
        [Command("pick")]
        [Alias("choose")]
        public async Task Pick([Remainder]string args)
        {
            if (Service.CheckBinding(Context.Channel as ITextChannel))
            {
                await ReplyAsync("This command is currently not available.");
                return;
            }

            var choices = args.Split(new[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
            var choice = choices[new Random().Next(choices.Length)].Trim();

            var embed = new EmbedBuilder()
            .WithTitle($"I choose '{choice}'.")
            .WithColor(ModuleColor);

            await Context.Channel.SendEmbedAsync(embed);
        }

        /// <summary>
        /// Command to roll a number of dice with a number of sides.
        /// </summary>
        /// <param name="arg">A string containing the roll in standard roll notation.</param>
        /// <returns></returns>
        [Command("roll")]
        [Alias("rolldice")]
        public async Task RollDice(string arg)
        {
            if (Service.CheckBinding(Context.Channel as ITextChannel))
            {
                await ReplyAsync("This command is currently not available.");
                return;
            }

            var rng = new Random();
            if (!int.TryParse(arg.Split('d')[0], out var multiplier))
            {
                await ReplyAsync("I could not understand how many dice you wanted to roll.");
                return;
            }

            if (!int.TryParse(arg.Split('d')[1], out var dieSize))
            {
                await ReplyAsync("I could not understand how many sides your dice were supposed to have.");
                return;
            }

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

        /// <summary>
        /// Command to print the lenny face in the chat.
        /// </summary>
        /// <returns></returns>
        [Command("lenny")]
        public async Task LennyFace()
        {
            if (Service.CheckBinding(Context.Channel as ITextChannel))
            {
                await ReplyAsync("This command is currently not available.");
                return;
            }

            var deleteAsync = Context.Message.DeleteAsync();
            await ReplyAsync("( ͡° ͜ʖ ͡°)");
            await deleteAsync;
        }

        /// <summary>
        /// Command group to manage restrictions to commands from the fun module.
        /// </summary>
        [Group("fun")]
        public class FunRestrictionModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FunRestrictionModule"/> class.
            /// </summary>
            /// <param name="svc">Backing service instance.</param>
            public FunRestrictionModule(FunService svc)
            {
                Service = svc;
            }

            /// <summary>
            /// Command to enable fun commands in this channel.
            /// </summary>
            /// <returns></returns>
            [Command("enable")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Enable()
            {
                var channel = Context.Channel as ITextChannel;
                if (Service.AddBinding(channel))
                    await ReplyAsync($"Usage of fun commands and responses in this channel (#{channel.Name}) is no longer blocked.");
                else
                    await ReplyAsync("Usage of fun commands and responses in this channel was not blocked.");
            }

            /// <summary>
            /// Command to disable fun commands.
            /// </summary>
            /// <returns></returns>
            [Command("disable")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Disable()
            {
                var channel = Context.Channel as ITextChannel;
                if (Service.RemoveBinding(channel))
                    await ReplyAsync($"Usage of fun commands and responses in this channel (#{channel.Name}) is now blocked.");
                else
                    await ReplyAsync("Usage of fun commands and responses in this channel is already blocked.");
            }

            /// <summary>
            /// Command to check if fun commands are disabled.
            /// </summary>
            /// <returns></returns>
            [Command("check")]
            [RequireContext(ContextType.Guild)]
            public async Task Check()
            {
                var channel = Context.Channel as ITextChannel;
                if (Service.CheckBinding(channel))
                    await ReplyAsync("Usage of fun commands and responses in this channel is blocked.");
                else
                    await ReplyAsync("Usage of fun commands and responses in this channel is not restricted.");
            }
        }
    }
}
