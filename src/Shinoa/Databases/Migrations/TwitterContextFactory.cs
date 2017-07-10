// <copyright file="TwitterContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Factory for creating Twitter contexts.
    /// </summary>
    public class TwitterContextFactory : DbContextFactory, IDesignTimeDbContextFactory<TwitterContext>
    {
        /// <inheritdoc cref="IDesignTimeDbContextFactory{TContext}"/>
        public TwitterContext CreateDbContext(string[] args) => new TwitterContext(Options);
    }
}
