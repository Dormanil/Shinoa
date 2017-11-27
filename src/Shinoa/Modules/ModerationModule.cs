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
    using System.Threading;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;

    using Extensions;

    using Services;
    using Services.TimedServices;

    using static Databases.BadWordContext;

    /// <summary>
    /// Module for Moderative uses.
    /// </summary>
    public class ModerationModule : ModuleBase<SocketCommandContext>
    {
        private static readonly Dictionary<string, long> TimeUnits = new Dictionary<string, long>
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

        public ModerationService Service { get; set; }

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
            await this.ReplyEmbedAsync($"User {user.Username}#{user.Discriminator} has been banned by {Context.User.Mention}.");
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
            await this.ReplyEmbedAsync($"User {user.Username}#{user.Discriminator} has been kicked by {Context.User.Mention}.");
        }

        [Command("initMuteRole")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task InitMuteRole([Remainder] string roleName = "Muted")
        {
            if (Service.GetRole(Context.Guild) is IRole role)
            {
                await this.ReplyEmbedAsync($"This server already has a role to mute users registered, `{role.Name}`. Did you perchance mean to update the role?", Color.Red);
                return;
            }

            await CreateMuteRole(roleName);
        }

        [Command("updateMuteRole")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task UpdateMuteRole([Remainder] string roleName = "Muted")
        {
            var role = Service.GetRole(Context.Guild) as IRole;
            if (role == null)
            {
                await this.ReplyEmbedAsync($"This server has no role to mute users registered yet. Registering new role instead...", Color.Orange);
                await InitMuteRole(roleName);
                return;
            }

            if (role.Name == roleName)
            {
                await this.ReplyEmbedAsync($"A mute-role with the name `{roleName}` exists already. Nothing to update.", Color.Red);
                return;
            }

            switch (await Service.RemoveRole(Context.Guild, role))
            {
                case BindingStatus.Error:
                    await this.ReplyEmbedAsync("An unexpected error removing the old role has occured. Aborting...", Color.Red);
                    return;
                case BindingStatus.NotExisting:
                case BindingStatus.Removed:
                    break;
                default:
                    break;
            }

            await role.DeleteAsync();
            await CreateMuteRole(roleName);
        }

        [Command("updateMuteRole")]
        [RequireUserPermission(GuildPermission.ManageRoles)]
        public async Task RemoveMuteRole()
        {
            var role = Service.GetRole(Context.Guild);
            if (role == null)
            {
                await this.ReplyEmbedAsync("This server has no role to mute users registered yet. Nothing to remove.", Color.Red);
                return;
            }

            switch (await Service.RemoveRole(Context.Guild, role))
            {
                case BindingStatus.Error:
                    await this.ReplyEmbedAsync("An unexpected error removing the old role has occured. Aborting...", Color.Red);
                    return;
                case BindingStatus.NotExisting:
                case BindingStatus.Removed:
                default:
                    break;
            }

            await role.DeleteAsync();
            await this.ReplyEmbedAsync("The mute-role has been removed successfully.", Color.Green);
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

            var mutedRole = Service.GetRole(Context.Guild);
            if (mutedRole == null)
            {
                await this.ReplyEmbedAsync("This server has no role to mute users registered yet. Registering default role...", Color.Orange);
                await InitMuteRole();
                mutedRole = Service.GetRole(Context.Guild) ?? throw new Exception();
            }

            var duration = TimeSpan.Zero;
            try
            {
                duration = TimeSpan.FromSeconds(amount * TimeUnits[unitName]);
            }
            catch (KeyNotFoundException)
            {
            }

            if (duration == TimeSpan.Zero)
            {
                switch (await Service.AddMute(Context.Guild, user))
                {
                    case BindingStatus.Error:
                        await this.ReplyEmbedAsync("An unexpected error removing the old role has occured. Aborting...", Color.Red);
                        return;
                    case BindingStatus.AlreadyExists:
                        await this.ReplyEmbedAsync("The user is already muted. Aborting...", Color.Red);
                        return;
                    case BindingStatus.Added:
                    default:
                        break;
                }

                await user.AddRoleAsync(mutedRole);
                await delTask;
                await this.ReplyEmbedAsync($"User {user.Mention} has been {gagString} by {Context.User.Mention}.");
                return;
            }

            if (duration < TimeSpan.Zero)
            {
                await this.ReplyEmbedAsync($"User {user.Username}#{user.Discriminator} has not been {gagString}, since the duration of the {(gagString == "gagged" ? "gag" : "mute")} was negative.");
                return;
            }

            if (duration > TimeSpan.FromSeconds(30))
            {
                switch (await Service.AddMute(Context.Guild, user, DateTime.Now + duration))
                {
                    case BindingStatus.Error:
                        await this.ReplyEmbedAsync($"An unexpected error muting {user.Username}#{user.Discriminator} has occured. Aborting...", Color.Red);
                        return;
                    case BindingStatus.AlreadyExists:
                        await this.ReplyEmbedAsync("The user is already muted. Aborting...", Color.Red);
                        return;
                    case BindingStatus.Added:
                    default:
                        break;
                }
            }

            await user.AddRoleAsync(mutedRole);
            await delTask;
            await this.ReplyEmbedAsync($"User {user.Mention} has been {gagString} by {Context.User.Mention} for {amount} {unitName}.");

            if (duration <= TimeSpan.FromSeconds(30))
            {
                var autoUnmuteThread = new Thread(new ModerationService.AutoUnmuteService
                {
                    Channel = Context.Channel as ITextChannel,
                    Role = mutedRole,
                    TimeSpan = duration,
                    User = user,
                }.AutoUnmute);
                autoUnmuteThread.Start();
            }
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
            var mutedRole = Service.GetRole(Context.Guild) ?? throw new Exception();

            switch (await Service.RemoveMute(Context.Guild, user))
            {
                case BindingStatus.Error:
                    await this.ReplyEmbedAsync($"An unexpected error unmuting {user.Username}#{user.Discriminator} has occured. Aborting...", Color.Red);
                    return;
                case BindingStatus.NotExisting:
                    await this.ReplyEmbedAsync($"The user is not muted. Aborting...", Color.Red);
                    return;
                case BindingStatus.Removed:
                default:
                    break;
            }

            await user.RemoveRoleAsync(mutedRole);
            await delTask;
            await ReplyAsync($"User {user.Mention} has been {gagString} by {Context.User.Mention}.");
        }

        private async Task CreateMuteRole(string roleName)
        {
            IRole mutedRole;
            try
            {
                mutedRole = Context.Guild.Roles.First(guildRole => guildRole.Name == roleName);
            }
            catch (InvalidOperationException)
            {
                var permissions = Context.Guild.EveryoneRole.Permissions.Modify(readMessages: false);
                var position = Context.Guild.Roles.Where(botRole => botRole.Members.Contains(Context.Guild.CurrentUser))
                                   .OrderBy(botRole => botRole.Position).First().Position + 1;
                mutedRole = await Context.Guild.CreateRoleAsync(roleName, permissions, Color.DarkRed);
                await mutedRole.ModifyAsync(prop => prop.Position = position);
            }

            await Service.AddRole(Context.Guild, mutedRole);
            await Context.Message.DeleteAsync();
            await this.ReplyEmbedAsync($"{roleName} role has been set up successfully.", Color.Green);
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

                await this.ReplyEmbedAsync("Sending to this channel has been restricted.", new Color(244, 67, 54));
                var newPerms = channel.GetPermissionOverwrite(channel.Guild.EveryoneRole)?.Modify(sendMessages: PermValue.Deny) ?? default(OverwritePermissions).Modify(sendMessages: PermValue.Deny);
                await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole, newPerms);
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

                await channel.AddPermissionOverwriteAsync(Context.User, default);
                var newPerms = channel.GetPermissionOverwrite(channel.Guild.EveryoneRole)?.Modify(sendMessages: PermValue.Inherit) ?? default(OverwritePermissions).Modify(sendMessages: PermValue.Inherit);
                await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole, newPerms);
                await this.ReplyEmbedAsync("Sending to this channel has been unrestricted.", new Color(139, 195, 74));
            }

            /// <summary>
            /// Command group to restrict messaging in all channels of the server.
            /// </summary>
            [Group("all")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public class StopAllModule : ModuleBase<SocketCommandContext>
            {
                /// <summary>
                /// Command to restrict sending messages to all channels.
                /// </summary>
                /// <returns></returns>
                [Command("on")]
                [RequireContext(ContextType.Guild)]
                [RequireUserPermission(GuildPermission.ManageChannels)]
                public async Task On()
                {
                    await this.ReplyEmbedAsync("Sending to all channels has been restricted.", new Color(244, 67, 54));
                    Context.Guild.Channels.ForEach(async channel =>
                    {
                        var newPerms =
                            channel.GetPermissionOverwrite(channel.Guild.EveryoneRole)
                                ?.Modify(sendMessages: PermValue.Deny) ??
                            default(OverwritePermissions).Modify(sendMessages: PermValue.Deny);
                        await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole, newPerms);
                        await channel.AddPermissionOverwriteAsync(Context.User, new OverwritePermissions(sendMessages: PermValue.Allow));
                    });
                }

                /// <summary>
                /// Command to revoke an earlier restriction to all channels.
                /// </summary>
                /// <returns></returns>
                [Command("off")]
                [RequireContext(ContextType.Guild)]
                [RequireUserPermission(GuildPermission.ManageChannels)]
                public async Task Off()
                {
                    Context.Guild.Channels.ForEach(async channel =>
                    {
                        await channel.AddPermissionOverwriteAsync(Context.User, default);
                        var newPerms =
                            channel.GetPermissionOverwrite(channel.Guild.EveryoneRole)
                                ?.Modify(sendMessages: PermValue.Inherit) ??
                            default(OverwritePermissions).Modify(sendMessages: PermValue.Inherit);
                        await channel.AddPermissionOverwriteAsync(channel.Guild.EveryoneRole, newPerms);
                    });

                    await this.ReplyEmbedAsync("Sending to all channels has been unrestricted.", new Color(139, 195, 74));
                }
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
            public ImageSpamService Service { get; set; }

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
                    await this.ReplyEmbedAsync($"Image spam in this channel (#{channel.Name}) is now blocked.");
                else
                    await this.ReplyEmbedAsync("Image spam in this channel is already blocked.");
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
                    await this.ReplyEmbedAsync($"Image spam in this channel (#{channel.Name}) is no longer blocked.");
                else
                    await this.ReplyEmbedAsync("Image spam in this channel was not blocked.");
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
                    await this.ReplyEmbedAsync("Image spam in this channel is blocked. Sending more than three images within 15 seconds will get you muted.");
                else
                    await this.ReplyEmbedAsync("Image spam in this channel is not restricted.");
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
                    await this.ReplyEmbedAsync($"Command usage on this server is now blocked for {user.Mention}.");
                else
                    await this.ReplyEmbedAsync("Command usage on this server is already blocked for that user.");
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
                    await this.ReplyEmbedAsync($"Command usage on this server is now no longer blocked for {user.Mention}.");
                else
                    await this.ReplyEmbedAsync("Command usage on this server was not blocked for that user.");
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
                    await this.ReplyEmbedAsync($"Command usage on this server is currently blocked for {user.Mention}.");
                else
                    await this.ReplyEmbedAsync("Command usage on this server is currently not blocked for that user.");
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
                var embed = new EmbedBuilder()
                    .AddField(f =>
                    {
                        f.WithName("The following words or expressions are banned on this server:");
                        f.WithValue(bindings
                            .Where(b => b.Key.isGuild && (b.Key.entity as BadWordServerBinding)?.ServerId ==
                                        Context.Guild.Id)
                            .SelectMany(b => b.Value)
                            .Aggregate((total, next) => $"{total}\n{next}"));
                    });

                bindings
                    .Where(b => !b.Key.isGuild && (b.Key.entity as BadWordChannelBinding)?.ServerId == Context.Guild.Id)
                    .ForEach(b =>
                    {
                        embed.AddField(f =>
                        {
                            f.WithName(
                                $"Banned in Channel <#{(b.Key.entity as BadWordChannelBinding)?.ChannelId ?? 0ul}>:");
                            f.WithValue(b.Value
                                .Aggregate((total, next) => $"{total}\n{next}"));
                        });
                    });

                embed.WithFooter(Shinoa.VersionString);
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
