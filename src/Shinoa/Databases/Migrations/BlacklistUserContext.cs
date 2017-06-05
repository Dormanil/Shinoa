// <copyright file="BlacklistUserContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public class BlacklistUserContextFactory : DbContextFactory, IDbContextFactory<BlacklistUserContext>
    {
        public BlacklistUserContext Create(string[] args) => new BlacklistUserContext(options);
    }
}
