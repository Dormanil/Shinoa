// <copyright file="DisjunctivePreconditionAttribute.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Discord.Commands;

    // FIXME: Doesn't actually work, because you simply can't put Attributes into Attributes.

    /// <summary>
    /// Attribute for declaring disjunctive permission requirements.
    /// </summary>
    [Obsolete]
    public class DisjunctivePreconditionAttribute : PreconditionAttribute
    {
        private readonly IEnumerable<PreconditionAttribute> preconditions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisjunctivePreconditionAttribute"/> class.
        /// </summary>
        /// <param name="preconditions">A number of preconditions, one of which must be fulfilled to fulfil the overarching precondition.</param>
        public DisjunctivePreconditionAttribute(params PreconditionAttribute[] preconditions)
        {
            this.preconditions = preconditions;
        }

        /// <summary>
        /// Checks permissions for using this command.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="command">The command used.</param>
        /// <param name="map">The map of dependencies.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider map) =>
            Task.FromResult(preconditions.Any(precondition => precondition.CheckPermissions(context, command, map).Result.IsSuccess)
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("You lack the necessary permissions to use this command."));
    }
}