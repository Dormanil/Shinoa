// <copyright file="IDatabaseService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System.Threading.Tasks;
    using Discord;

    public interface IDatabaseService : IService
    {
        Task<bool> RemoveBinding(IEntity<ulong> binding);
    }
}