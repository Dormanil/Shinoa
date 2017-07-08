// <copyright file="RequireGuildUserPermissionAttribute.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Attributes
{
    using System;
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Attribute for declaring permission requirements for users in guilds.
    /// </summary>
    public class RequireGuildUserPermissionAttribute : RequireUserPermissionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequireGuildUserPermissionAttribute"/> class.
        /// </summary>
        /// <param name="permission">Guild-wide permissions.</param>
        public RequireGuildUserPermissionAttribute(GuildPermission permission)
            : base(permission) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireGuildUserPermissionAttribute"/> class.
        /// </summary>
        /// <param name="permission">Channel-wide permissions.</param>
        public RequireGuildUserPermissionAttribute(ChannelPermission permission)
            : base(permission) { }

        /// <summary>
        /// Checks permissions for using this command.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="command">The command used.</param>
        /// <param name="map">The map of dependencies.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider map)
        {
            if (context.Guild == null) return PreconditionResult.FromSuccess();
            return await base.CheckPermissions(context, command, map);
        }
    }
}