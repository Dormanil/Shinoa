// <copyright file="TwitterContext.cs" company="The Shinoa Development Team">
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

    public class TwitterContext : DbContext, IDatabaseContext
    {
        public TwitterContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<TwitterBinding> TwitterBindingSet { get; set; }

        public DbSet<TwitterChannelBinding> DbSet { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("twitter");
            modelBuilder.Entity<TwitterChannelBinding>()
                .HasKey(b => new { b.TwitterBinding, b.ChannelId });
        }

        public class TwitterChannelBinding
        {
            public TwitterBinding TwitterBinding { get; set; }

            public ulong ChannelId { get; set; }
        }

        public class TwitterBinding
        {
            [Key]
            public string TwitterUsername { get; set; }

            public DateTime LatestPost { get; set; }
        }
    }
}
