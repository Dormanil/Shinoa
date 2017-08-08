// <copyright file="ModerationService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services.TimedServices
{
    using System;
    using System.Threading.Tasks;

    using Discord;

    public class ModerationService : ITimedService, IDatabaseService
    {
        /// <inheritdoc />
        public void Init(dynamic config, IServiceProvider map)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task Callback()
        {
            throw new NotImplementedException();
        }
    }
}
