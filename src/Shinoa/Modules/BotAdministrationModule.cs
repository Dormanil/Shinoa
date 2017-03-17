// <copyright file="BotAdministrationModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    /// <summary>
    /// Module for administrative tasks concerning the bot itself.
    /// </summary>
    public class BotAdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient client;
        private readonly CommandService commandService;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotAdministrationModule"/> class.
        /// </summary>
        /// <param name="clnt">Client running the bot.</param>
        /// <param name="commandSvc">Backing service instance.</param>
        public BotAdministrationModule(DiscordSocketClient clnt, CommandService commandSvc)
        {
            client = clnt;
            commandService = commandSvc;
        }

        /// <summary>
        /// Command to set the avatar of the bot.
        /// </summary>
        /// <param name="url">URL to the image.</param>
        /// <returns></returns>
        [Command("setavatar")]
        [Alias("avatar")]
        [RequireOwner]
        public async Task SetAvatar(string url)
        {
            var splitLink = Regex.Match(url, @"(\S+)\/(\S+)").Groups;
            if (!splitLink[0].Success) return;
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };
            var stream = await (await new HttpClient(handler) { BaseAddress = new Uri(splitLink[1].Value) }.GetAsync(splitLink[2].Value))
                .Content.ReadAsStreamAsync();
            await client.CurrentUser.ModifyAsync(p =>
            {
                p.Avatar = new Image(stream);
            });
        }

        /// <summary>
        /// Command to set the status of the bot.
        /// </summary>
        /// <param name="game">Status message.</param>
        /// <returns></returns>
        [Command("setplaying")]
        [Alias("setstatus", "game", "status")]
        [RequireOwner]
        public async Task SetPlaying([Remainder]string game)
        {
            await client.SetGameAsync(game);
        }

        /// <summary>
        /// Command to get statistics.
        /// </summary>
        /// <returns></returns>
        [Command("stats")]
        [Alias("diag", "statistics")]
        [RequireOwner]
        public async Task GetStats()
        {
            await ReplyAsync(GenerateStatsMessage());
        }

        /// <summary>
        /// Command to make a public announcement using the bot.
        /// </summary>
        /// <param name="announcement">An announcement message.</param>
        /// <returns></returns>
        [Command("announce")]
        [Alias("announcement", "global")]
        [RequireOwner]
        public async Task Announce([Remainder]string announcement)
        {
            foreach (IGuild server in client.Guilds)
            {
                await (await server.GetDefaultChannelAsync()).SendMessageAsync($"**Announcement:** {announcement}");
            }
        }

        /// <summary>
        /// Command to say something using the bot's voice.
        /// </summary>
        /// <param name="message">The message to say.</param>
        /// <returns></returns>
        [Command("say")]
        [RequireOwner]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task Say([Remainder]string message)
        {
            var replyTask = ReplyAsync(message);
            if (Context.Channel is IGuildChannel) await Context.Message.DeleteAsync();
            await replyTask;
        }

        /// <summary>
        /// Command to print an invite link for the bot.
        /// </summary>
        /// <returns></returns>
        [Command("invite")]
        [RequireOwner]
        public async Task GetInviteLink()
        {
            await ReplyAsync($"Invite link for {client.CurrentUser.Mention}: https://discordapp.com/oauth2/authorize?client_id={client.CurrentUser.Id}&scope=bot");
        }

        [Command("shutdown")]
        [Alias("kill")]
        [RequireOwner]
        public async Task Shutdown()
        {
            await ReplyAsync("Shutting down.");
            Shinoa.Cts.CancelAfter(2000);
        }

        private string GenerateStatsMessage()
        {
            var output = string.Empty;
            output += $"**Shinoa v{Shinoa.Version}**\n";

            var computerName = Environment.MachineName;
            var uptime = DateTime.Now - Shinoa.StartTime;
            var uptimeString = $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes, {uptime.Seconds} seconds.";

            output += $"Running on {computerName}\n";
            output += $"Uptime: {uptimeString}\n\n";

            output += "Running modules:\n\n```";
            output = commandService.Modules.Aggregate(output, (current, module) => current + $"{module.Name}\n");
            output += "```";

            return output;
        }
    }
}
