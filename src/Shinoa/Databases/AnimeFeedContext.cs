// <copyright file="AnimeFeedContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    public class AnimeFeedContext : DbContext, IDatabaseContext
    {
        public AnimeFeedContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<AnimeFeedBinding> AnimeFeedBindings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("animefeed");
        }

        public class AnimeFeedBinding
        {
            public static DateTime LatestPost { get; set; } = DateTime.UtcNow;

            [Key]
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
