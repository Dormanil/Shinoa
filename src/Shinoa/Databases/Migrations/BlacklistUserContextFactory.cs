// <copyright file="BlacklistUserContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Factory for creating user blacklist contexts.
    /// </summary>
    public class BlacklistUserContextFactory : DbContextFactory, IDesignTimeDbContextFactory<BlacklistUserContext>
    {
        /// <inheritdoc cref="IDesignTimeDbContextFactory{TContext}"/>
        public BlacklistUserContext CreateDbContext(string[] args) => new BlacklistUserContext(Options);
    }
}
