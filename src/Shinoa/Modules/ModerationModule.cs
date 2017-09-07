// <copyright file="ModerationModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;
    using Services;
    using static Databases.BadWordContext;

    /// <summary>
    /// Module for Moderative uses.
    /// </summary>
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Dictionary<string, int> TimeUnits = new Dictionary<string, int>
        {
            { "second",    1000 },
            { "seconds",   1000 },
            { "minute",    1000 * 60 },
            { "minutes",   1000 * 60 },
            { "hour",      1000 * 60 * 60 },
            { "hours",     1000 * 60 * 60 },
            { "day",       1000 * 60 * 60 * 24 },
            { "days",      1000 * 60 * 60 * 24 },
        };

        /// <summary>
        /// Command to ban a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <param name="pruneDays">How many days of messages to prune.</param>
        /// <param name="reason">The reason for banning the user.</param>
        /// <returns></returns>
        [Command("ban")]
        [Alias("gulag", "getout")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task Ban(IUser user, int pruneDays = 0, [Remainder] string reason = null)
        {
            if (Context.Guild == null) return;
            var delTask = Context.Message.DeleteAsync();

            if (pruneDays < 0) pruneDays = 0;
            else if (pruneDays > 7) pruneDays = 7;
            await Context.Guild.AddBanAsync(user, pruneDays, reason);
            await delTask;
            await ReplyAsync($"User {user.Username} has been banned by {Context.User.Mention}.");
        }

        /// <summary>
        /// Command to kick a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <param name="reason">The reason for kicking the user.</param>
        /// <returns></returns>
        [Command("kick")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task Kick(IGuildUser user, [Remainder] string reason = null)
        {
            var delTask = Context.Message.DeleteAsync();

            var kickTask = user.KickAsync(reason);
            await delTask;
            if (kickTask == null) return;
            await kickTask;
            await ReplyAsync($"User {user.Username} has been kicked by {Context.User.Mention}.");
        }

        /// <summary>
        /// Command to mute a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <param name="amount">A timespan in full timeunits.</param>
        /// <param name="unitName">The name of the unit.</param>
        /// <returns></returns>
        [Command("mute")]
        [Alias("gag")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Mute(IGuildUser user, int amount = 0, string unitName = "")
        {
            var gagString = Context.Message.Content.Contains("gag") && !user.Nickname.Contains("gag")
                ? "gagged"
                : "muted";

            var delTask = Context.Message.DeleteAsync();

            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));
            var duration = 0;
            try
            {
                duration = amount * TimeUnits[unitName];
            }
            catch (KeyNotFoundException)
            {
            }

            if (duration == 0)
            {
                await user.AddRoleAsync(mutedRole);
                await delTask;
                await ReplyAsync($"User {user.Mention} has been {gagString} by {Context.User.Mention}.");
                return;
            }

            if (duration < 0)
            {
                await ReplyAsync($"User <@{user.Id}> has not been {gagString}, since the duration of the {(gagString == "gagged" ? "gag" : "mute")} was negative.");
                return;
            }

            await user.AddRoleAsync(mutedRole);
            await delTask;
            await ReplyAsync($"User {user.Mention} has been {gagString} by {Context.User.Mention} for {amount} {unitName}.");
            await Task.Delay(duration);

            await user.RemoveRoleAsync(mutedRole);
            await ReplyAsync($"User <@{user.Id}> has been un{gagString} automatically.");
        }

        /// <summary>
        /// Command to unmute a user.
        /// </summary>
        /// <param name="user">The user in question.</param>
        /// <returns></returns>
        [Command("unmute")]
        [Alias("ungag")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task Unmute(IGuildUser user)
        {
            var gagString = Context.Message.Content.Contains("ungag") && !user.Nickname.Contains("ungag")
                ? "ungagged"
                : "unmuted";

            var delTask = Context.Message.DeleteAsync();
            IRole mutedRole = Context.Guild.Roles.FirstOrDefault(role => role.Name.ToLower().Contains("muted"));

            await user.RemoveRoleAsync(mutedRole);
            await delTask;
            await ReplyAsync($"User {user.Mention} has been {gagString} by {Context.User.Mention}.");
        }

        /// <summary>
        /// Command group to manage temporary restrictions to channels.
        /// </summary>
        [Group("stop")]
        public class StopModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Command to restrict sending messages to the channel.
            /// </summary>
            /// <returns></returns>
            [Command("on")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task On()
            {
                var channel = (IGuildChannel)Context.Channel;

                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been restricted.").WithColor(new Color(244, 67, 54));
                await ReplyAsync(string.Empty, embed: embed.Build());
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, new OverwritePermissions(sendMessages: PermValue.Deny, addReactions: PermValue.Deny));
                await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(sendMessages: PermValue.Allow));
            }

            /// <summary>
            /// Command to revoke an earlier restriction to the channel.
            /// </summary>
            /// <returns></returns>
            [Command("off")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Off()
            {
                var channel = (IGuildChannel)Context.Channel;

                await channel.AddPermissionOverwriteAsync(Context.User, default(OverwritePermissions));
                await channel.AddPermissionOverwriteAsync(Context.Guild.EveryoneRole, default(OverwritePermissions));
                var embed = new EmbedBuilder().WithTitle("Sending to this channel has been unrestricted.").WithColor(new Color(139, 195, 74));
                await ReplyAsync(string.Empty, embed: embed.Build());
            }
        }

        /// <summary>
        /// Command group to manage image spamming.
        /// </summary>
        [Group("imagespam")]
        [RequireContext(ContextType.Guild)]
        public class ImageSpamModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Gets or sets the backing service instance.
            /// </summary>
            public ModerationService Service { get; set; }

            /// <summary>
            /// Command to block image spam in this channel.
            /// </summary>
            /// <returns></returns>
            [Command("block")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Block()
            {
                var channel = Context.Channel as ITextChannel;
                if (await Service.RemoveBinding(channel))
                    await ReplyAsync($"Image spam in this channel (#{channel.Name}) is now blocked.");
                else
                    await ReplyAsync("Image spam in this channel is already blocked.");
            }

            /// <summary>
            /// Command to revoke a prior image spam block.
            /// </summary>
            /// <returns></returns>
            [Command("unblock")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task Unblock()
            {
                var channel = Context.Channel as ITextChannel;
                if (await Service.AddBinding(channel))
                    await ReplyAsync($"Image spam in this channel (#{channel.Name}) is no longer blocked.");
                else
                    await ReplyAsync("Image spam in this channel was not blocked.");
            }

            /// <summary>
            /// Command to check if image spam is being blocked.
            /// </summary>
            /// <returns></returns>
            [Command("check")]
            public async Task Check()
            {
                var channel = Context.Channel as ITextChannel;
                if (!Service.CheckBinding(channel))
                    await ReplyAsync("Image spam in this channel is blocked. Sending more than three images within 15 seconds will get you muted.");
                else
                    await ReplyAsync("Image spam in this channel is not restricted.");
            }
        }

        /// <summary>
        /// Command group to manage command usage for specific users.
        /// </summary>
        [Group("blacklist")]
        [RequireContext(ContextType.Guild)]
        public class BlacklistModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Gets or sets the backing service instance.
            /// </summary>
            public BlacklistService Service { get; set; }

            /// <summary>
            /// Command to add a specific user to the bot's blacklist.
            /// </summary>
            /// <param name="user">The user in question.</param>
            /// <returns></returns>
            [Command("add")]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task Add(IGuildUser user)
            {
                if (await Service.AddBinding(Context.Guild, user))
                    await ReplyAsync($"Command usage on this server is now blocked for {user.Mention}.");
                else
                    await ReplyAsync("Command usage on this server is already blocked for that user.");
            }

            /// <summary>
            /// Command to remove a specific user from the bot's blacklist.
            /// </summary>
            /// <param name="user">The user in question.</param>
            /// <returns></returns>
            [Command("remove")]
            [RequireUserPermission(GuildPermission.MuteMembers)]
            public async Task Remove(IGuildUser user)
            {
                if (await Service.RemoveBinding(Context.Guild, user))
                    await ReplyAsync($"Command usage on this server is now no longer blocked for {user.Mention}.");
                else
                    await ReplyAsync("Command usage on this server was not blocked for that user.");
            }

            /// <summary>
            /// Command to check if a specific user is on the bot's blacklist.
            /// </summary>
            /// <param name="user">The user in question.</param>
            /// <returns></returns>
            [Command("check")]
            public async Task Check(IGuildUser user = null)
            {
                if (user == null) user = (IGuildUser)Context.User;
                if (Service.CheckBinding(Context.Guild, user))
                    await ReplyAsync($"Command usage on this server is currently blocked for {user.Mention}.");
                else
                    await ReplyAsync("Command usage on this server is currently not blocked for that user.");
            }
        }

        /// <summary>
        /// Command group to add, remove and list blacklisted words.
        /// </summary>
        [Group("badword")]
        [RequireContext(ContextType.Guild)]
        public class BadWordModule : ModuleBase<SocketCommandContext>
        {
            /// <summary>
            /// Gets or sets the backing service instance.
            /// </summary>
            public BadWordService Service { get; set; }

            /// <summary>
            /// Lists bad words blacklisted in each channel of the server and globally on the server.
            /// </summary>
            /// <returns></returns>
            [Command("list")]
            public async Task List()
            {
                var bindings = Service.ListBindings(Context);

                var serverBadWords = bindings
                    .Where(b => b.Key.isGuild && (b.Key.entity as BadWordServerBinding)?.ServerId == Context.Guild.Id)
                    .SelectMany(b => b.Value)
                    .Aggregate("The following words or expressions are banned on this server:```", (total, next) => total + $"\n  - {next}") + "\n```";
                var channelBadWords = bindings
                    .Where(b => !b.Key.isGuild && (b.Key.entity as BadWordChannelBinding)?.ServerId == Context.Guild.Id)
                    .Aggregate("The following words or expressions are banned in the following channels:```", (total, next) => total + $"\n#{(Context.Client.GetChannel((next.Key.entity as BadWordChannelBinding)?.ChannelId ?? 0ul) as ITextChannel)?.Name}:{next.Value.Aggregate(string.Empty, (channelTotal, channelNext) => channelTotal + $"\n  - {channelNext}")}\n```");

                var embed = new EmbedBuilder
                {
                    Description = serverBadWords + "\n\n" + channelBadWords,
                };

                await ReplyAsync(string.Empty, embed: embed);
            }

            /// <summary>
            /// Command group to add bad word bindings.
            /// </summary>
            [Group("add")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public class AddBadWordModule : ModuleBase<SocketCommandContext>
            {
                /// <summary>
                /// Gets or sets the backing service instance.
                /// </summary>
                public BadWordService Service { get; set; }

                /// <summary>
                /// Adds a bad word to the list of filtered words for the channel the command was used in.
                /// </summary>
                /// <param name="badWord">Bad word to add to the filtered words.</param>
                /// <returns></returns>
                [Command]
                [Priority(0)]
                public async Task Add([Remainder]string badWord)
                {
                    switch (await Service.AddBinding(false, Context, badWord.RemovePunctuation()))
                    {
                        case BindingStatus.AlreadyExists:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it are already blocked in this channel.");
                            break;
                        case BindingStatus.Added:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it are now blocked in this channel.");
                            break;
                        case BindingStatus.Error:
                        default:
                            throw new Exception("!badword add command failed due to an unknown error.");
                    }
                }

                /// <summary>
                /// Adds a bad word to the list of filtered words for the guild the command was used in.
                /// </summary>
                /// <param name="badWord">Bad word to add to the filtered words.</param>
                /// <returns></returns>
                [Command("global")]
                [Priority(10)]
                [RequireUserPermission(GuildPermission.ManageGuild)]
                public async Task AddGlobal([Remainder]string badWord)
                {
                    switch (await Service.AddBinding(true, Context, badWord.RemovePunctuation()))
                    {
                        case BindingStatus.AlreadyExists:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it are already blocked on this server.");
                            break;
                        case BindingStatus.Added:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it are now blocked on this server.");
                            break;
                        case BindingStatus.Error:
                        default:
                            throw new Exception("!badword add global command failed due to an unknown error.");
                    }
                }
            }

            /// <summary>
            /// Command group to remove bad word bindings.
            /// </summary>
            [Group("remove")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public class RemoveBadWordModule : ModuleBase<SocketCommandContext>
            {
                /// <summary>
                /// Gets or sets the backing service instance.
                /// </summary>
                public BadWordService Service { get; set; }

                /// <summary>
                /// Removes a bad word from the list of filtered words for the channel the command was used in.
                /// </summary>
                /// <param name="badWord">Bad word to add to the filtered words.</param>
                /// <returns></returns>
                [Command]
                [Priority(0)]
                public async Task Remove([Remainder]string badWord)
                {
                    switch (await Service.RemoveBinding(false, Context, badWord.RemovePunctuation()))
                    {
                        case BindingStatus.NotExisting:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it were not blocked in this channel.");
                            break;
                        case BindingStatus.Removed:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it are now no longer blocked in this channel.");
                            break;
                        case BindingStatus.Error:
                        default:
                            throw new Exception($"{(string)Shinoa.Config["global"]["command_prefix"]}badword remove command failed due to an unknown error.");
                    }
                }

                /// <summary>
                /// Removes a bad word from the list of filtered words for the guild the command was used in.
                /// </summary>
                /// <param name="badWord">Bad word to add to the filtered words.</param>
                /// <returns></returns>
                [Command("global")]
                [Priority(10)]
                [RequireUserPermission(GuildPermission.ManageGuild)]
                public async Task RemoveGlobal([Remainder]string badWord)
                {
                    switch (await Service.RemoveBinding(true, Context, badWord.RemovePunctuation()))
                    {
                        case BindingStatus.NotExisting:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it were not blocked on this server.");
                            break;
                        case BindingStatus.Removed:
                            await ReplyAsync($"Badword `{badWord}` and messages containing it are now no longer blocked on this server.");
                            break;
                        case BindingStatus.Error:
                        default:
                            throw new Exception($"{(string)Shinoa.Config["global"]["command_prefix"]}badword remove global command failed due to an unknown error.");
                    }
                }
            }
        }
    }
}
