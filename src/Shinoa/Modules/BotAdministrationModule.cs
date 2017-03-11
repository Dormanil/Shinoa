using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Net.Http;
using System.IO;
using System.Net;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class BotAdministrationModule : ModuleBase<SocketCommandContext>
    {
        [Command("setavatar"), Alias("avatar"), RequireOwner]
        public async Task SetAvatar(string url)
        {
            var splitLink = System.Text.RegularExpressions.Regex.Match(url, @"(\S+)\/(\S+)").Groups;
            if (!splitLink[0].Success) return;
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            }; 
            var stream = await (await new HttpClient (handler) { BaseAddress = new Uri(splitLink[1].Value) }.GetAsync(splitLink[2].Value))
                .Content.ReadAsStreamAsync();
            await Shinoa.DiscordClient.CurrentUser.ModifyAsync(p =>
            {
                p.Avatar = new Image(stream);
            });
        }

        [Command("setplaying"), Alias("setstatus", "game", "status"), RequireOwner]
        public async Task SetPlaying([Remainder]string game)
        {
            await Shinoa.DiscordClient.SetGameAsync(game);
        }

        [Command("stats"), Alias("diag", "statistics"), RequireOwner]
        public void GetStats()
        {
            ReplyAsync(GenerateStatsMessage());
        }

        [Command("announce"), Alias("announcement", "global"), RequireOwner]
        public async Task Announce([Remainder]string announcement)
        {
            foreach (IGuild server in Shinoa.DiscordClient.Guilds)
            {
                await (await server.GetDefaultChannelAsync()).SendMessageAsync($"**Announcement:** {announcement}");
            }
        }

        [Command("say"), RequireOwner, RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task Say([Remainder]string message)
        {
            var replyTask = ReplyAsync(message);
            if(Context.Channel is IGuildChannel) await Context.Message.DeleteAsync();
            await replyTask;
        }

        [Command("invite"), RequireOwner]
        public async Task GetInviteLink()
        {
            await ReplyAsync($"Invite link for {Shinoa.DiscordClient.CurrentUser.Mention}: https://discordapp.com/oauth2/authorize?client_id={Shinoa.DiscordClient.CurrentUser.Id}&scope=bot");
        }

        string GenerateStatsMessage()
        {
            var output = "";
            output += $"**Shinoa v{Shinoa.Version}**\n";

            var computerName = Environment.MachineName;
            var uptime = (DateTime.Now - Shinoa.StartTime);
            var uptimeString = $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes, {uptime.Seconds} seconds.";

            output += $"Running on {computerName}\n";
            output += $"Uptime: {uptimeString}\n\n";

            output += "Running modules:\n\n```";
            foreach (var module in Shinoa.Commands.Modules)
            {
                output += $"{module.Name}\n";
            }
            output += "```";

            return output;
        }
    }
}
