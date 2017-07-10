// <copyright file="RedditContext.cs" company="The Shinoa Development Team">
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
    /// A <see cref="DbContext"/> for managing reddit feeds.
    /// </summary>
    public class RedditContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public RedditContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for subreddit bindings.
        /// </summary>
        public DbSet<RedditBinding> RedditBindings { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for channel to subreddits bindings.
        /// </summary>
        public DbSet<RedditChannelBinding> RedditChannelBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("reddit");
            modelBuilder.Entity<RedditChannelBinding>()
                .HasKey(b => new { b.SubredditName, b.ChannelIdString });
        }

        /// <summary>
        /// Bindings keeping track of a subreddit, its latest post, and channels subscribed to the feed.
        /// </summary>
        public class RedditBinding
        {
            /// <summary>
            /// Gets or sets the name of the subreddit.
            /// </summary>
            [Key]
            public string SubredditName { get; set; }

            /// <summary>
            /// Gets or sets the time of the latest post.
            /// </summary>
            public DateTimeOffset LatestPost { get; set; }

            /// <summary>
            /// Gets or sets the list of channels subscribed to the subreddit.
            /// </summary>
            public List<RedditChannelBinding> ChannelBindings { get; set; }
        }

        /// <summary>
        /// Bindings keeping track of a subscribed subreddit and the subscribing channel.
        /// </summary>
        public class RedditChannelBinding
        {
            /// <summary>
            /// Gets or sets the subscribed subreddit.
            /// </summary>
            [ForeignKey("SubredditName")]
            public RedditBinding Subreddit { get; set; }

            /// <summary>
            /// Gets or sets the name of the subreddit.
            /// </summary>
            public string SubredditName { get; set; }

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
