// <copyright file="JoinPartServerContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Factory for creating server joining/parting contexts.
    /// </summary>
    public class JoinPartServerContextFactory : DbContextFactory, IDesignTimeDbContextFactory<JoinPartServerContext>
    {
        /// <inheritdoc cref="IDesignTimeDbContextFactory{TContext}"/>
        public JoinPartServerContext CreateDbContext(string[] args) => new JoinPartServerContext(Options);
    }
}
