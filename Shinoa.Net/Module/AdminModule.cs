using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;
using System.Reflection;

namespace Shinoa.Net.Module
{
    class AdminModule : IModule
    {   
        public void Init()
        {
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.User.Id.ToString().Equals(ShinoaNet.Config["owner_id"]))
            {
                if (Convenience.ContainsBotMention(e.Message.RawText) || e.Message.Channel.IsPrivate)
                {
                    var cleanMessage = Convenience.RemoveMentions(e.Message.RawText).Trim().ToLower();

                    if (cleanMessage.Equals("stats"))
                    {
                        Logging.LogMessage(e.Message);

                        var computerName = Environment.MachineName;
                        var userName = Environment.UserName;
                        var uptime = (DateTime.Now - ShinoaNet.StartTime);
                        var uptimeString = $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes, {uptime.Seconds} seconds.";

                        var modulesString = "";

                        foreach (var module in ShinoaNet.ActiveModules)
                        {
                            modulesString += $"{module.GetType().Name}\n";

                            if (module.DetailedStats() != null)
                            {
                                var indentedDetailedStats = "";

                                using (StringReader reader = new StringReader(module.DetailedStats()))
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

                                modulesString += indentedDetailedStats;
                            }
                        }

                        modulesString = modulesString.Trim();

                        e.Channel.SendMessage($"**Shinoa.Net ver. {ShinoaNet.VersionId}**\nRunning as {userName} @ {computerName}\nUptime: {uptimeString}\n\n```\nActive modules:\n\n{modulesString}\n```");
                    }
                    else if (cleanMessage.StartsWith("set game"))
                    {
                        Logging.LogMessage(e.Message);

                        var game = Convenience.RemoveMentions(e.Message.RawText).Replace("set game", "").Trim();
                        ShinoaNet.DiscordClient.SetGame(game);

                        Logging.Log($"Set game to '{game}'.");
                    }
                    else if (cleanMessage.StartsWith("bind anime notifications"))
                    {
                        Logging.LogMessage(e.Message);

                        AnimeNotificationsModule.BoundChannels.Add(e.Channel);

                        Logging.Log($"Bound anime notifications to {e.Server.Name} -> #{e.Channel.Name}.");
                        e.Channel.SendMessage($"Bound anime notifications to #{e.Channel.Name}.");
                    }
                    else if (cleanMessage.Equals("restart"))
                    {
                        // Starts a new instance of the program itself
                        System.Diagnostics.Process.Start(Assembly.GetExecutingAssembly().Location);

                        // Closes the current process
                        Environment.Exit(0);
                    }
                }
            }            
        }

        public string DetailedStats()
        {
            return null;
        }
    }
}
