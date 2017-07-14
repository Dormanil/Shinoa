// <copyright file="BadWordContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Factory for creating badword contexts.
    /// </summary>
    public class BadWordContextFactory : DbContextFactory, IDesignTimeDbContextFactory<BadWordContext>
    {
        /// <inheritdoc cref="IDesignTimeDbContextFactory{TContext}"/>
        public BadWordContext CreateDbContext(string[] args) => new BadWordContext(Options);
    }
}
