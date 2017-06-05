// <copyright file="RedditContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public class RedditContextFactory : DbContextFactory, IDbContextFactory<RedditContext>
    {
        public RedditContext Create(string[] args) => new RedditContext(options);
    }
}
