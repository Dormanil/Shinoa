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

    /// <summary>
    /// A <see cref="DbContext"/> for managing twitter feeds.
    /// </summary>
    public class TwitterContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public TwitterContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for twitter user bindings.
        /// </summary>
        public DbSet<TwitterBinding> TwitterBindings { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for twitter user to channel bindings.
        /// </summary>
        public DbSet<TwitterChannelBinding> TwitterChannelBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("twitter");
            modelBuilder.Entity<TwitterChannelBinding>()
                .HasKey(b => new { b.TwitterUsername, b.ChannelIdString });
        }

        /// <summary>
        /// Bindings keeping track of a twitter user, its latest tweet, and channels subscribed to the feed.
        /// </summary>
        public class TwitterBinding
        {
            /// <summary>
            /// Gets or sets the name of the twitter user.
            /// </summary>
            [Key]
            public string TwitterUsername { get; set; }

            /// <summary>
            /// Gets or sets the time of the latest post.
            /// </summary>
            public DateTime LatestPost { get; set; }

            /// <summary>
            /// Gets or sets the list of channels subscribed to the twitter feed.
            /// </summary>
            public List<TwitterChannelBinding> ChannelBindings { get; set; }
        }

        /// <summary>
        /// Bindings keeping track of a subscribed twitter user feed and the subscribing channel.
        /// </summary>
        public class TwitterChannelBinding
        {
            /// <summary>
            /// Gets or sets the subscribed twitter user feed.
            /// </summary>
            [ForeignKey("TwitterUsername")]
            public TwitterBinding TwitterBinding { get; set; }

            /// <summary>
            /// Gets or sets the name of the twitter user.
            /// </summary>
            public string TwitterUsername { get; set; }

            /// <summary>
            /// Gets or sets the channel ID string.
            /// </summary>
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
