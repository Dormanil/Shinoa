// <copyright file="AnimeFeedContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// A <see cref="DbContext"/> for the Anime Feed.
    /// </summary>
    public class AnimeFeedContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public AnimeFeedContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the AnimeFeedBinding database set.
        /// </summary>
        public DbSet<AnimeFeedBinding> AnimeFeedBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("animefeed");
        }

        /// <summary>
        /// Bindings for the Anime Feed.
        /// </summary>
        public class AnimeFeedBinding
        {
            /// <summary>
            /// Gets or sets the time of the latest post.
            /// </summary>
            public static DateTime LatestPost { get; set; } = DateTime.UtcNow;

            /// <summary>
            /// Gets or sets the channel ID string.
            /// </summary>
            [Key]
            public string ChannelIdString { get; set; }

            /// <summary>
            /// Gets or sets the channel ID, backed by <see cref="ChannelIdString"/>.
            /// </summary>
            [NotMapped]
            public ulong ChannelId
            {
                get => ulong.Parse(ChannelIdString);
                set => ChannelIdString = value.ToString();
            }
        }
    }
}
