using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class ModerationModule : Abstract.Module
    {
        static Dictionary<string, int> TimeUnits = new Dictionary<string, int>()
        {
            { "seconds",    1000 },
            { "minutes",    1000 * 60 },
            { "hours",      1000 * 60 * 60 }
        };

        public override void Init()
        {
            this.BoundCommands.Add("ban", (e) =>
            {
                if (e.User.ServerPermissions.BanMembers)
                {
                    e.Message.Delete();

                    var user = e.Server.GetUser(Util.IdFromMention(GetCommandParameters(e.Message.RawText)[0]));
                    e.Server.Ban(user);                    
                    e.Channel.SendMessage($"User {user.Name} has been banned by <@{e.User.Id}>.");
                }
                else
                {
                    e.Channel.SendPermissionError("Ban Members");
                }
            });

            this.BoundCommands.Add("kick", (e) =>
            {
                if (e.User.ServerPermissions.KickMembers)
                {
                    e.Message.Delete();

                    var user = e.Server.GetUser(Util.IdFromMention(GetCommandParameters(e.Message.RawText)[0]));
                    user.Kick();
                    e.Channel.SendMessage($"User {user.Name} has been kicked by <@{e.User.Id}>.");
                }
                else
                {
                    e.Channel.SendPermissionError("Kick Members");
                }
            });

            this.BoundCommands.Add("mute", async (e) =>
            {
                if (e.User.ServerPermissions.MuteMembers)
                {
                    await e.Message.Delete();

                    Role mutedRole = null;
                    foreach (var role in e.Server.Roles)
                    {
                        if (role.Name.ToLower().Contains("muted"))
                        {
                            mutedRole = role;
                            break;
                        }
                    }

                    var user = e.Server.GetUser(Util.IdFromMention(GetCommandParameters(e.Message.RawText)[0]));
                    await user.AddRoles(mutedRole);

                    if (GetCommandParameters(e.Message.Text).Count() == 1)
                    {
                        await e.Channel.SendMessage($"User <@{user.Id}> has been muted by <@{e.User.Id}>.");
                    }
                    else if (GetCommandParameters(e.Message.Text).Count() == 3)
                    {
                        var amount = int.Parse(GetCommandParameters(e.Message.Text)[1]);
                        var unitName = GetCommandParameters(e.Message.Text)[2].Trim().ToLower();

                        var timeDuration = amount * TimeUnits[unitName];

                        await e.Channel.SendMessage($"User <@{user.Id}> has been muted by <@{e.User.Id}> for {amount} {unitName}.");
                        await Task.Delay(timeDuration);
                        await user.RemoveRoles(mutedRole);
                        await e.Channel.SendMessage($"User <@{user.Id}> has been unmuted automatically.");
                    }
                }
                else
                {
                    e.Channel.SendPermissionError("Mute Members");
                }
            });

            this.BoundCommands.Add("unmute", (e) =>
            {
                if (e.User.ServerPermissions.MuteMembers)
                {
                    e.Message.Delete();

                    Role mutedRole = null;
                    foreach (var role in e.Server.Roles)
                    {
                        if (role.Name.ToLower().Contains("muted"))
                        {
                            mutedRole = role;
                            break;
                        }
                    }

                    var user = e.Server.GetUser(Util.IdFromMention(GetCommandParameters(e.Message.RawText)[0]));
                    user.RemoveRoles(mutedRole);

                    if (GetCommandParameters(e.Message.Text).Count() == 1)
                    {
                        e.Channel.SendMessage($"User <@{user.Id}> has been unmuted by <@{e.User.Id}>.");
                    }
                }
                else
                {
                    e.Channel.SendPermissionError("Mute Members");
                }
            });
        }
    }
}
