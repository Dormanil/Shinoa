using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Text.RegularExpressions;
using System.Timers;

namespace Shinoa.Net.Module
{
    class ModerationModule : IModule
    {
        public class ModerationException : Exception
        {
            public ModerationException()
            {
            }

            public ModerationException(string message) : base(message)
            {
            }

            public ModerationException(string message, Exception inner) : base(message, inner)
            {
            }
        }

        static string[] CommandList = { "ban", "kick", "mute", "unmute", "isauthorized" };

        static Dictionary<string, int> TimeUnits = new Dictionary<string, int>()
        {
            { "seconds",    1000 },
            { "minutes",    1000 * 60 },
            { "hours",      1000 * 60 * 60 }
        };

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            foreach (var command in CommandList)
            {
                if (e.Message.Text.Trim().ToLower().StartsWith(ShinoaNet.Config["command_prefix"] + command))
                {
                    try
                    {
                        dynamic serverConfig = null;
                        foreach (var currentServerConfig in ShinoaNet.Config["moderator_config"])
                        {
                            if (ulong.Parse(currentServerConfig["server"]) == e.Server.Id) serverConfig = currentServerConfig;
                        }

                        if (serverConfig == null) throw new ModerationException("Not configured for this server.");

                        Role modRole = e.Server.GetRole(ulong.Parse(serverConfig["role"]));

                        var authorized = false;
                        foreach (var role in e.User.Roles)
                        {
                            if (role.Id == modRole.Id)
                            {
                                authorized = true;
                                break;
                            }
                        }

                        if (!authorized) throw new ModerationException($"User does not have the `{modRole.Name}` role.");

                        var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"(?<querytext>.*)");
                        if (regex.IsMatch(e.Message.Text))
                        {
                            var commandText = regex.Matches(e.Message.RawText)[0].Groups["querytext"].Value;

                            if (commandText.StartsWith("mute") || commandText.StartsWith("unmute"))
                            {                                
                                Role mutedRole = null;
                                foreach (var role in e.Server.Roles)
                                {
                                    if (role.Name == serverConfig["muted_role_name"])
                                    {
                                        mutedRole = role;
                                        break;
                                    }
                                }

                                var segments = commandText.Split(new char[] { ' ' });
                                var userId = ulong.Parse(segments[1].Substring(3, segments[1].Length - 4));

                                if (commandText.StartsWith("mute"))
                                {
                                    var userToMute = e.Server.GetUser(userId);
                                    userToMute.AddRoles(mutedRole);

                                    if (segments.Length > 2)
                                    {
                                        var length = int.Parse(segments[2]);
                                        var unit = segments[3];

                                        var unmuteTimer = new Timer();
                                        unmuteTimer.Interval = TimeUnits[unit] * length;
                                        unmuteTimer.Elapsed += (ss, ee) => { userToMute.RemoveRoles(mutedRole); };
                                        unmuteTimer.AutoReset = false;
                                        unmuteTimer.Start();

                                        e.Channel.SendMessage($"Muted user <@{userId}> for {length} {unit}.");
                                    }
                                    else
                                    {
                                        e.Channel.SendMessage($"Muted user <@{userId}>.");
                                    }
                                }
                                else if (commandText.StartsWith("unmute"))
                                {
                                    var userToUnmute = e.Server.GetUser(userId);
                                    userToUnmute.RemoveRoles(mutedRole);

                                    e.Channel.SendMessage($"Unmuted user <@{userId}>.");
                                }
                            }
                            else if (commandText.StartsWith("ban"))
                            {
                                var segments = commandText.Split(new char[] { ' ' });
                                var userId = ulong.Parse(segments[1].Substring(3, segments[1].Length - 4));

                                e.Server.Ban(e.Server.GetUser(userId));
                                e.Channel.SendMessage("User has been banned.");
                            }
                            else if (commandText.StartsWith("kick"))
                            {
                                var segments = commandText.Split(new char[] { ' ' });
                                var userId = ulong.Parse(segments[1].Substring(3, segments[1].Length - 4));

                                e.Server.GetUser(userId).Kick();
                                e.Channel.SendMessage("User has been kicked.");
                            }
                            else if (commandText == "isauthorized")
                            {
                                e.Channel.SendMessage("You are authorized to use moderation commands.");
                            }
                        }
                    }
                    catch (ModerationException ex)
                    {
                        e.Channel.SendMessage(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        e.Channel.SendMessage("Unexpected error.");
                        Console.WriteLine(ex);
                        Console.WriteLine("Command message was: " + e.Message.RawText);
                    }
                }
            }
        }
    }
}
