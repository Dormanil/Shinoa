// <copyright file="RequireGuildUserPermissionAttribute.cs" company="The Shinoa Development Team">
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

    public class RequireGuildUserPermissionAttribute : RequireUserPermissionAttribute
    {
        public RequireGuildUserPermissionAttribute(GuildPermission permission)
            : base(permission) { }

        public RequireGuildUserPermissionAttribute(ChannelPermission permission)
            : base(permission) { }

        public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IDependencyMap map)
        {
            if (context.Guild == null) return PreconditionResult.FromSuccess();
            return await base.CheckPermissions(context, command, map);
        }
    }
}