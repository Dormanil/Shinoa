// <copyright file="BadWordContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    public class BadWordContextFactory : DbContextFactory, IDesignTimeDbContextFactory<BadWordContext>
    {
        public BadWordContext CreateDbContext(string[] args) => new BadWordContext(Options);
    }
}
