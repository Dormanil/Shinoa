using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Net.Http;
using System.IO;
using Discord.Commands;
using Shinoa.Attributes;

namespace Shinoa.Modules
{
    public class BotAdministrationModule : Abstract.Module
    {
        [@Command("setavatar", "avatar")]
        public async Task SetAvatar(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                var stream = new HttpClient().GetAsync(args[0]).Result.Content.ReadAsStreamAsync().Result;
                await Shinoa.DiscordClient.CurrentUser.ModifyAsync(p =>
                {
                    p.Avatar = new Image(stream);
                });
            }
        }

        [@Command("setplaying", "setstatus", "game", "status")]
        public async Task SetPlaying(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                await Shinoa.DiscordClient.SetGameAsync(args.ToRemainderString());
            }
        }

        [@Command("stats", "diag", "statistics")]
        public async Task GetStats(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                await c.Channel.SendMessageAsync(GenerateStatsMessage());
            }
        }

        [@Command("announce", "announcement", "global")]
        public async Task Announce(CommandContext c, params string[] args)
        {
            if (c.User.Id != ulong.Parse(Shinoa.Config["owner_id"])) return;
            var announcement = args.ToRemainderString();

                foreach (var server in Shinoa.DiscordClient.Guilds)
                {
                    await server.GetDefaultChannelAsync().Result.SendMessageAsync($"**Announcement:** {announcement}");
                }
            }
        }

        [@Command("say")]
        public async Task Say(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                var message = args.ToRemainderString();
                await c.Message.DeleteAsync();
                await c.Channel.SendMessageAsync(message);
            }
        }

        [@Command("invite")]
        public async Task GetInviteLink(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                await c.Channel.SendMessageAsync($"Invite link for {Shinoa.DiscordClient.CurrentUser.Mention}: https://discordapp.com/oauth2/authorize?client_id=198527882773921792&scope=bot");
            }
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
            foreach (var module in Shinoa.RunningModules)
            {
                output += $"{module.GetType().Name}\n";
                var indentedDetailedStats = "";

                if (module.DetailedStats == null) continue;
                using (StringReader reader = new StringReader(module.DetailedStats))
                {
                    string line = string.Empty;
                    do
                    {
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            indentedDetailedStats += "  " + line + '\n';
                        }

                    } while (line != null);
                }

                output += $"{indentedDetailedStats}\n";
            }
            output += "```";

            return output;
        }
    }
}
