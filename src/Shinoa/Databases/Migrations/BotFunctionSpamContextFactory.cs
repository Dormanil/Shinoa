// <copyright file="BotFunctionSpamContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    /// <summary>
    /// Factory for creating botfunctionspam contexts.
    /// </summary>
    public class BotFunctionSpamContextFactory : DbContextFactory, IDesignTimeDbContextFactory<BotFunctionSpamContext>
    {
        /// <inheritdoc cref="IDesignTimeDbContextFactory{TContext}"/>
        public BotFunctionSpamContext CreateDbContext(string[] args) => new BotFunctionSpamContext(Options);
    }
}
