// <copyright file="BotAdministrationModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
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
        /// <summary>
        /// Gets or sets the backing Discord client instance.
        /// </summary>
        public DiscordSocketClient Client { get; set; }

        /// <summary>
        /// Gets or sets the backing service instance.
        /// </summary>
        public CommandService CommandService { get; set; }

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
            await Client.CurrentUser.ModifyAsync(p =>
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
            await Client.SetGameAsync(game);
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
            foreach (IGuild server in Client.Guilds)
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
        [RequireBotPermission(GuildPermission.ManageMessages)]
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
            await ReplyAsync($"Invite link for {Client.CurrentUser.Mention}: https://discordapp.com/oauth2/authorize?client_id={Client.CurrentUser.Id}&scope=bot");
        }

        /// <summary>
        /// Command to shut the bot down.
        /// </summary>
        /// <returns></returns>
        [Command("shutdown")]
        [Alias("kill")]
        [RequireOwner]
        public async Task Shutdown()
        {
            await ReplyAsync("Shutting down.");
            Shinoa.Cts.CancelAfter(2000);
        }

        /// <summary>
        /// Command to leave the server.
        /// </summary>
        /// <param name="args">Optional: ID of the Guild to leave.</param>
        /// <returns></returns>
        [Command("leave")]
        [RequireOwner]
        public async Task Leave([Remainder]string args = null)
        {
            try
            {
                var guildId = args != null ? ulong.Parse(args) : Context.Guild.Id;
                if (!await Shinoa.TryLeaveGuildAsync(guildId))
                {
                    await ReplyAsync("Failed to leave server.");
                    return;
                }

                await Logging.Log($"Left server with id {guildId}.");
            }
            catch (FormatException)
            {
                await Logging.LogError($"{args} could not be parsed.");
            }
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
            output = CommandService.Modules.Aggregate(output, (current, module) => current + $"{module.Name}\n");
            output += "```";

            return output;
        }
    }
}
