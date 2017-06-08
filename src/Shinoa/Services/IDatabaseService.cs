// <copyright file="IDatabaseService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using Discord;

    public interface IDatabaseService : IService
    {
        bool RemoveBinding(IEntity<ulong> binding);
    }
}