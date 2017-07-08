// <copyright file="TwitterContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    public class TwitterContext : DbContext, IDatabaseContext
    {
        public TwitterContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<TwitterBinding> TwitterBindings { get; set; }

        public DbSet<TwitterChannelBinding> TwitterChannelBindings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("twitter");
            modelBuilder.Entity<TwitterChannelBinding>()
                .HasKey(b => new { b.TwitterUsername, b.ChannelIdString });
        }

        public class TwitterBinding
        {
            [Key]
            public string TwitterUsername { get; set; }

            public DateTime LatestPost { get; set; }

            public List<TwitterChannelBinding> ChannelBindings { get; set; }
        }

        public class TwitterChannelBinding
        {
            [ForeignKey("TwitterUsername")]
            public TwitterBinding TwitterBinding { get; set; }

            public string TwitterUsername { get; set; }

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
