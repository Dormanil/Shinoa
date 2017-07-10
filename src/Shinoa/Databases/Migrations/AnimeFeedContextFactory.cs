// <copyright file="AnimeFeedContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Factory for creating Anime Feed Contexts.
    /// </summary>
    public class AnimeFeedContextFactory : DbContextFactory, IDesignTimeDbContextFactory<AnimeFeedContext>
    {
        /// <inheritdoc cref="IDesignTimeDbContextFactory{TContext}"/>
        public AnimeFeedContext CreateDbContext(string[] args) => new AnimeFeedContext(Options);
    }
}
