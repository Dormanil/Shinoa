// <copyright file="RequireGuildBotPermissionAttribute.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Attributes
{
    using System.Threading.Tasks;
    using Discord;
    using Discord.Commands;

    /// <summary>
    /// Attribute for declaring permission requirements for bots in guilds.
    /// </summary>
    public class RequireGuildBotPermissionAttribute : RequireBotPermissionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequireGuildBotPermissionAttribute"/> class.
        /// </summary>
        /// <param name="permission">Guild-wide permissions.</param>
        public RequireGuildBotPermissionAttribute(GuildPermission permission)
            : base(permission) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireGuildBotPermissionAttribute"/> class.
        /// </summary>
        /// <param name="permission">Channel-wide permissions.</param>
        public RequireGuildBotPermissionAttribute(ChannelPermission permission)
            : base(permission) { }

        /// <summary>
        /// Checks permissions for using this command.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="command">The command used.</param>
        /// <param name="map">The map of dependencies.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            if (context.Guild == null) return PreconditionResult.FromSuccess();
            return await base.CheckPermissions(context, command, map);
        }
    }
}
