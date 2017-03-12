using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Shinoa.Attributes;
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

        [@Command("ban", "gulag", "getout")]
        public async Task Ban(CommandContext c, params string[] args)
        {
            if ((c.User as SocketGuildUser).GuildPermissions.BanMembers)
            {
                await c.Message.DeleteAsync();

                var user = c.Guild.GetUserAsync(Util.IdFromMention(args[0])).Result;
                await c.Guild.AddBanAsync(user);
                var banmessage = c.Message.Content;
                string blurb;
                if (banmessage.StartsWith($"{Shinoa.Config["command_prefix"]}gulag"))
                {
                    blurb = "was sent to work in a Sibirian gulag on orders of";
                }
                else if (banmessage.StartsWith($"{Shinoa.Config["command_prefix"]}getout"))
                {
                    blurb = "found their way out thanks to";
                }
                else
                {
                    blurb = "was banned by";
                }
                await c.Channel.SendMessageAsync($"User {user.Username} {blurb} {c.User.Mention}.");
            }
            else
            {
                await c.Channel.SendPermissionErrorAsync("Ban Members");
            }
        }

        [@Command("kick")]
        public async Task Kick(CommandContext c, params string[] args)
        {
            if ((c.User as SocketGuildUser).GuildPermissions.KickMembers)
            {
                await c.Message.DeleteAsync();

                var user = c.Guild.GetUserAsync(Util.IdFromMention(args[0])).Result;
                await user.KickAsync();
                await c.Channel.SendMessageAsync($"User {user.Username} has been kicked by {c.User.Mention}.");
            }
            else
            {
                await c.Channel.SendPermissionErrorAsync("Kick Members");
            }
        }

        [@Command("mute")]
        public async void Mute(CommandContext c, params string[] args)
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

                var user = c.Guild.GetUserAsync(Util.IdFromMention(args[0])).Result;
                await user.AddRolesAsync(mutedRole);

                if (args.Count() == 1)
                {
                    await c.Channel.SendMessageAsync($"User {user.Mention} has been muted by {c.User.Mention}.");
                }
                else if (args.Count() == 3)
                {
                    var amount = int.Parse(args[1]);
                    var unitName = args[2].Trim().ToLower();

                    var timeDuration = amount * TimeUnits[unitName];

                    await c.Channel.SendMessageAsync($"User {user.Mention} has been muted by {c.User.Mention} for {amount} {unitName}.");
                    await Task.Delay(timeDuration);
                    await user.RemoveRolesAsync(mutedRole);
                    await c.Channel.SendMessageAsync($"User <@{user.Id}> has been unmuted automatically.");
                }
            }
            else
            {
                await c.Channel.SendPermissionErrorAsync("Mute Members");
            }
        }

        [@Command("unmute")]
        public async Task Unmute(CommandContext c, params string[] args)
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

                var user = c.Guild.GetUserAsync(Util.IdFromMention(args[0])).Result;
                await user.RemoveRolesAsync(mutedRole);

                if (args.Count() == 1)
                {
                    await c.Channel.SendMessageAsync($"User {user.Mention} has been unmuted by {c.User.Mention}.");
                }
            }
            else
            {
                await c.Channel.SendPermissionErrorAsync("Mute Members");
            }
        }

        [@Command("stop")]
        public async Task StopChannel(CommandContext c, params string[] args)
        {
            if ((c.User as SocketGuildUser).GuildPermissions.ManageChannels)
            {
                var channel = c.Channel as IGuildChannel;

                if (args[0] == "on")
                {
                    var embed = new EmbedBuilder().WithTitle("Sending to this channel has been restricted.").WithColor(new Color(244, 67, 54));
                    await c.Channel.SendEmbedAsync(embed.Build());
                    await channel.AddPermissionOverwriteAsync(c.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
                    await channel.AddPermissionOverwriteAsync(c.User, new OverwritePermissions(sendMessages: PermValue.Allow));
                }
                else if (args[0] == "off")
                {
                    await channel.AddPermissionOverwriteAsync(c.User, new OverwritePermissions(sendMessages: PermValue.Inherit));
                    await channel.AddPermissionOverwriteAsync(c.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Inherit));
                    var embed = new EmbedBuilder().WithTitle("Sending to this channel has been unrestricted.").WithColor(new Color(139, 195, 74));
                    await c.Channel.SendEmbedAsync(embed.Build());
                }
            }
        }
        /*
        public override async void HandleMessage(CommandContext context)
        {
            try
            {
                if (context.Channel.Id == context.Guild?.DefaultChannelId && context.Message.Attachments.Count > 0)
                {
                    var imagesCounter = 0;
                    context.Channel.GetMessagesAsync(limit: 50).ForEach(mlist =>
                    {
                        foreach (var message in mlist.ToList().OrderByDescending(o => o.Timestamp))
                        {
                            var timeDifference = DateTimeOffset.Now - message.Timestamp;
                            if (timeDifference.TotalSeconds < 15)
                            {
                                if (message.Attachments.Count > 0 && message.Author.Id == context.User.Id)
                                {
                                    imagesCounter++;
                                }
                            }
                        }
                    });

                    if (imagesCounter > 2)
                    {
                        await context.Message.DeleteAsync();
                        await context.Channel.SendMessageAsync($"{context.User.Mention} Your message has been removed for being image spam. You have been preventively muted.");

                        IRole mutedRole = null;
                        foreach (var role in context.Guild.Roles)
                        {
                            if (role.Name.ToLower().Contains("muted"))
                            {
                                mutedRole = role;
                                break;
                            }
                        }

                        await (context.User as SocketGuildUser).AddRolesAsync(mutedRole);
                        await Task.Delay(5 * 60 * 1000);
                        await (context.User as SocketGuildUser).RemoveRolesAsync(mutedRole);
                        await context.Channel.SendMessageAsync($"User {context.User.Mention} has been unmuted automatically.");
                    }
                }
            }
            catch (HttpException)
            {
                return;
            }
        }
        */
    }
}
