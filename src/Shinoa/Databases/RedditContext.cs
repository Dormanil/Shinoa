// <copyright file="RedditContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    public class RedditContext : DbContext, IDatabaseContext
    {
        public RedditContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<RedditBinding> RedditBindings { get; set; }

        public DbSet<RedditChannelBinding> RedditChannelBindings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("reddit");
            modelBuilder.Entity<RedditChannelBinding>()
                .HasKey(b => new { b.SubredditName, b.ChannelIdString });
        }

        public class RedditBinding
        {
            [Key]
            public string SubredditName { get; set; }

            public DateTimeOffset LatestPost { get; set; }

            public List<RedditChannelBinding> ChannelBindings { get; set; }
        }

        public class RedditChannelBinding
        {
            [ForeignKey("SubredditName")]
            public RedditBinding Subreddit { get; set; }

            public string SubredditName { get; set; }

            public string ChannelIdString { get; set; }

            [NotMapped]
            public ulong ChannelId
            {
                get => ulong.Parse(ChannelIdString);
                set => ChannelIdString = value.ToString();
            }
        }
    }
}
