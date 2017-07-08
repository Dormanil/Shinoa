// <copyright file="TwitterContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    public class TwitterContextFactory : DbContextFactory, IDesignTimeDbContextFactory<TwitterContext>
    {
        public TwitterContext CreateDbContext(string[] args) => new TwitterContext(Options);
    }
}
