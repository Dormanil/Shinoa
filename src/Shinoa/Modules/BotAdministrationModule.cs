using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Net.Http;
using System.IO;

namespace Shinoa.Modules
{
    public class BotAdministrationModule : Abstract.Module
    {
        public override void Init()
        {
            this.BoundCommands.Add("setavatar", (c) =>
            {
                if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
                {
                    var stream = new HttpClient().GetAsync(GetCommandParameters(c.Message.Content)[0]).Result.Content.ReadAsStreamAsync().Result;
                    Shinoa.DiscordClient.CurrentUser.ModifyAsync(p =>
                    {
                        p.Avatar = new Image(stream);
                    });
                }
            });
                
            this.BoundCommands.Add("setplaying", (c) =>
            {
                if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
                {
                    Shinoa.DiscordClient.SetGameAsync(GetCommandParametersAsString(c.Message.Content));
                }
            });

            this.BoundCommands.Add("stats", (c) =>
            {
                if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
                {
                    c.Channel.SendMessageAsync(GenerateStatsMessage());
                }
            });

            this.BoundCommands.Add("announce", (c) =>
            {
                if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
                {
                    var announcement = GetCommandParametersAsString(c.Message.Content);

                    foreach (var server in Shinoa.DiscordClient.Guilds)
                    {
                        server.GetDefaultChannelAsync().Result.SendMessageAsync($"**Announcement:** {announcement}");
                    }
                }
            });

            this.BoundCommands.Add("say", (c) =>
            {
                if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
                {
                    var message = GetCommandParametersAsString(c.Message.Content);
                    c.Message.DeleteAsync();
                    c.Channel.SendMessageAsync(message);
                }
            });
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
