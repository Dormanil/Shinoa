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
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;

    public class BotAdministrationModule : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient client;
        private readonly CommandService commandService;

        public BotAdministrationModule(DiscordSocketClient clnt, CommandService commandSvc)
        {
            this.client = clnt;
            this.commandService = commandSvc;
        }

        [Command("setavatar")]
        [Alias("avatar")]
        [RequireOwner]
        public async Task SetAvatar(string url)
        {
            var splitLink = System.Text.RegularExpressions.Regex.Match(url, @"(\S+)\/(\S+)").Groups;
            if (!splitLink[0].Success) return;
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };
            var stream = await (await new HttpClient(handler) { BaseAddress = new Uri(splitLink[1].Value) }.GetAsync(splitLink[2].Value))
                .Content.ReadAsStreamAsync();
            await this.client.CurrentUser.ModifyAsync(p =>
            {
                p.Avatar = new Image(stream);
            });
        }

        [Command("setplaying")]
        [Alias("setstatus", "game", "status")]
        [RequireOwner]
        public async Task SetPlaying([Remainder]string game)
        {
            await this.client.SetGameAsync(game);
        }

        [Command("stats")]
        [Alias("diag", "statistics")]
        [RequireOwner]
        public async Task GetStats()
        {
            await this.ReplyAsync(this.GenerateStatsMessage());
        }

        [Command("announce")]
        [Alias("announcement", "global")]
        [RequireOwner]
        public async Task Announce([Remainder]string announcement)
        {
            foreach (IGuild server in this.client.Guilds)
            {
                await (await server.GetDefaultChannelAsync()).SendMessageAsync($"**Announcement:** {announcement}");
            }
        }

        [Command("say")]
        [RequireOwner]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task Say([Remainder]string message)
        {
            var replyTask = this.ReplyAsync(message);
            if (this.Context.Channel is IGuildChannel) await this.Context.Message.DeleteAsync();
            await replyTask;
        }

        [Command("invite")]
        [RequireOwner]
        public async Task GetInviteLink()
        {
            await this.ReplyAsync($"Invite link for {this.client.CurrentUser.Mention}: https://discordapp.com/oauth2/authorize?client_id={this.client.CurrentUser.Id}&scope=bot");
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
            output = this.commandService.Modules.Aggregate(output, (current, module) => current + $"{module.Name}\n");
            output += "```";

            return output;
        }
    }
}
