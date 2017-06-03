// <copyright file="RedditContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.ComponentModel.DataAnnotations;

    using Microsoft.EntityFrameworkCore;

    public class RedditContext : DbContext, IDatabaseContext
    {
        public RedditContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("reddit");
            modelBuilder.Entity<RedditChannelBinding>()
                .HasKey(b => new { b.Subreddit, b.ChannelId });
        }

        public DbSet<RedditBinding> RedditBindingSet { get; set; }

        public DbSet<RedditChannelBinding> DbSet { get; set; }

        public class RedditBinding
        {
            [Key]
            public string SubredditName { get; set; }

            public DateTimeOffset LatestPost { get; set; }
        }

        public class RedditChannelBinding
        {
            public RedditBinding Subreddit { get; set; }

            public ulong ChannelId { get; set; }
        }
    }
}
