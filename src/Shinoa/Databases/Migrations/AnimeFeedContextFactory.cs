// <copyright file="AnimeFeedContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public class AnimeFeedContextFactory : DbContextFactory, IDbContextFactory<AnimeFeedContext>
    {
        public AnimeFeedContext Create(string[] args) => new AnimeFeedContext(options);
    }
}
