// <copyright file="AnimeFeedContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    public class AnimeFeedContextFactory : DbContextFactory, IDesignTimeDbContextFactory<AnimeFeedContext>
    {
        public AnimeFeedContext CreateDbContext(string[] args) => new AnimeFeedContext(Options);
    }
}
