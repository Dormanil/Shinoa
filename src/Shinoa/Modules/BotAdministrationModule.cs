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
        public void SetAvatar(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                var stream = new HttpClient().GetAsync(args[0]).Result.Content.ReadAsStreamAsync().Result;
                Shinoa.DiscordClient.CurrentUser.ModifyAsync(p =>
                {
                    p.Avatar = new Image(stream);
                });
            }
        }

        [@Command("setplaying", "setstatus", "game", "status")]
        public void SetPlaying(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                Shinoa.DiscordClient.SetGameAsync(args.ToRemainderString());
            }
        }

        [@Command("stats", "diag", "statistics")]
        public void GetStats(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                c.Channel.SendMessageAsync(GenerateStatsMessage());
            }
        }

        [@Command("announce", "announcement", "global")]
        public void Announce(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                var announcement = args.ToRemainderString();

                foreach (var server in Shinoa.DiscordClient.Guilds)
                {
                    server.GetDefaultChannelAsync().Result.SendMessageAsync($"**Announcement:** {announcement}");
                }
            }
        }

        [@Command("say")]
        public void Say(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                var message = args.ToRemainderString();
                c.Message.DeleteAsync();
                c.Channel.SendMessageAsync(message);
            }
        }

        [@Command("invite")]
        public void GetInviteLink(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                c.Channel.SendMessageAsync($"Invite link for {Shinoa.DiscordClient.CurrentUser.Mention}: https://discordapp.com/oauth2/authorize?client_id=198527882773921792&scope=bot");
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
                
                if (module.DetailedStats != null)
                {
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
            }
            output += "```";

            return output;
        }
    }
}
