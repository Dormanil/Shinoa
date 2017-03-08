using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        static readonly Dictionary<string, int> TimeUnits = new Dictionary<string, int>()
        {
            { "seconds",    1000 },
            { "minutes",    1000 * 60 },
            { "hours",      1000 * 60 * 60 }
        };

        [Command("ban"), Alias("gulag", "getout"), RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IUser user)
        {
            if (Context.Guild == null) return;
            var delTask = Context.Message.DeleteAsync();
            
            await Context.Guild.AddBanAsync(user);
            await delTask;
            await ReplyAsync($"User {user.Username} has been banned by {Context.User.Mention}.");
        }

        [Command("kick"), RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user)
        {
            var delTask = Context.Message.DeleteAsync();
            
            var kickTask = user.KickAsync();
            await delTask;
            if (kickTask == null) return;
            else await kickTask;
            await ReplyAsync($"User {user.Username} has been kicked by {Context.User.Mention}.");
        }

        [Command("mute"), RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Mute(IGuildUser user, int amount = 0, string unitName = "")
        {
            var delTask = Context.Message.DeleteAsync();

            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.AddRolesAsync(mutedRole);
            await delTask;

            if (amount == 0)
            {
                await ReplyAsync($"User {user.Mention} has been muted by {Context.User.Mention}.");
            }
            else
            {
                var duration = amount * TimeUnits[unitName];
                await ReplyAsync($"User {user.Mention} has been muted by {Context.User.Mention} for {amount} {unitName}.");
                await Task.Delay(duration);
                await user.RemoveRolesAsync(mutedRole);
                await ReplyAsync($"User <@{user.Id}> has been unmuted automatically.");
            }
        }

        [Command("unmute"), RequireUserPermission(GuildPermission.MuteMembers)]
        public async Task Unmute(IGuildUser user)
        {
            var delTask = Context.Message.DeleteAsync();
            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.RemoveRolesAsync(mutedRole);
            await delTask;
            await ReplyAsync($"User {user.Mention} has been unmuted by {Context.User.Mention}.");
        }

        [Command("stop"), RequireUserPermission(GuildPermission.ManageChannels)]
        public async Task StopChannel(string setting)
        {
            var channel = Context.Channel as IGuildChannel;

            if (setting == "on")
            {
                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been restricted.").WithColor(new Color(244, 67, 54));
                await ReplyAsync("", embed: embed.Build());
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(sendMessages: PermValue.Allow));
            }
            else if (setting == "off")
            {
                await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(sendMessages: PermValue.Inherit));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Inherit));
                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been unrestricted.").WithColor(new Color(139, 195, 74));
                await ReplyAsync("", embed: embed.Build());
            }
        }

        //TODO: Migrate
        public async Task HandleMessage(CommandContext context)
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
    }
}
