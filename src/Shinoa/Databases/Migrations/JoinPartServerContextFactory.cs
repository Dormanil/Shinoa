// <copyright file="JoinPartServerContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public class JoinPartServerContextFactory : DbContextFactory, IDbContextFactory<JoinPartServerContext>
    {
        public JoinPartServerContext Create(string[] args) => new JoinPartServerContext(options);
    }
}
