using Discord;
using Discord.WebSocket;
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
            this.BoundCommands.Add("ban", (c) =>
            {
                if ((c.User as SocketGuildUser).GuildPermissions.BanMembers)
                {
                    c.Message.DeleteAsync();

                    var user = c.Guild.GetUserAsync(Util.IdFromMention(GetCommandParameters(c.Message.Content)[0])).Result;
                    c.Guild.AddBanAsync(user);                    
                    c.Channel.SendMessageAsync($"User {user.Username} has been banned by {c.User.Mention}.");
                }
                else
                {
                    c.Channel.SendPermissionErrorAsync("Ban Members");
                }
            });

            this.BoundCommands.Add("kick", (c) =>
            {
                if ((c.User as SocketGuildUser).GuildPermissions.KickMembers)
                {
                    c.Message.DeleteAsync();

                    var user = c.Guild.GetUserAsync(Util.IdFromMention(GetCommandParameters(c.Message.Content)[0])).Result;
                    user.KickAsync();
                    c.Channel.SendMessageAsync($"User {user.Username} has been kicked by {c.User.Mention}.");
                }
                else
                {
                    c.Channel.SendPermissionErrorAsync("Kick Members");
                }
            });

            this.BoundCommands.Add("mute", async (c) =>
            {
                if ((c.User as SocketGuildUser).GuildPermissions.MuteMembers)
                {
                    await c.Message.DeleteAsync();

                    IRole mutedRole = null;
                    foreach (var role in c.Guild.Roles)
                    {
                        if (role.Name.ToLower().Contains("muted"))
                        {
                            mutedRole = role;
                            break;
                        }
                    }

                    var user = c.Guild.GetUserAsync(Util.IdFromMention(GetCommandParameters(c.Message.Content)[0])).Result;
                    await user.AddRolesAsync(mutedRole);

                    if (GetCommandParameters(c.Message.Content).Count() == 1)
                    {
                        await c.Channel.SendMessageAsync($"User {user.Mention} has been muted by {c.User.Mention}.");
                    }
                    else if (GetCommandParameters(c.Message.Content).Count() == 3)
                    {
                        var amount = int.Parse(GetCommandParameters(c.Message.Content)[1]);
                        var unitName = GetCommandParameters(c.Message.Content)[2].Trim().ToLower();

                        var timeDuration = amount * TimeUnits[unitName];

                        await c.Channel.SendMessageAsync($"User {user.Mention} has been muted by {c.User.Mention} for {amount} {unitName}.");
                        await Task.Delay(timeDuration);
                        await user.RemoveRolesAsync(mutedRole);
                        await c.Channel.SendMessageAsync($"User <@{user.Id}> has been unmuted automatically.");
                    }
                }
                else
                {
                    c.Channel.SendPermissionErrorAsync("Mute Members");
                }
            });

            this.BoundCommands.Add("unmute", (c) =>
            {
                if ((c.User as SocketGuildUser).GuildPermissions.MuteMembers)
                {
                    c.Message.DeleteAsync();
                    IRole mutedRole = null;
                    foreach (var role in c.Guild.Roles)
                    {
                        if (role.Name.ToLower().Contains("muted"))
                        {
                            mutedRole = role;
                            break;
                        }
                    }

                    var user = c.Guild.GetUserAsync(Util.IdFromMention(GetCommandParameters(c.Message.Content)[0])).Result;
                    user.RemoveRolesAsync(mutedRole);

                    if (GetCommandParameters(c.Message.Content).Count() == 1)
                    {
                        c.Channel.SendMessageAsync($"User {user.Mention} has been unmuted by {c.User.Mention}.");
                    }
                }
                else
                {
                    c.Channel.SendPermissionErrorAsync("Mute Members");
                }
            });
        }
    }
}
