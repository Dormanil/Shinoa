// <copyright file="BotFunctionSpamContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Design;

    public class BotFunctionSpamContextFactory : DbContextFactory, IDesignTimeDbContextFactory<BotFunctionSpamContext>
    {
        public BotFunctionSpamContext CreateDbContext(string[] args) => new BotFunctionSpamContext(Options);
    }
}
