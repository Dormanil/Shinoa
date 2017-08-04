// <copyright file="BadWordContext.cs" company="The Shinoa Development Team">
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
    using System.Linq;

    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// A <see cref="DbContext"/> for filtering bad words.
    /// </summary>
    public class BadWordContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext"/>
        public BadWordContext(DbContextOptions options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for channel based badword bindings.
        /// </summary>
        public DbSet<BadWordChannelBinding> BadWordChannelBindings { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for server based badword bindings.
        /// </summary>
        public DbSet<BadWordServerBinding> BadWordServerBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("badwordfilter")
                .Entity<ChannelBadWord>()
                .HasKey(b => new { b.ChannelIdString, b.Entry });

            modelBuilder
                .Entity<ChannelBadWord>()
                .HasIndex(b => b.ServerIdString);

            modelBuilder
                .Entity<ServerBadWord>()
                .HasKey(b => new { b.ServerIdString, b.Entry });

            modelBuilder
                .Entity<BadWordChannelBinding>()
                .HasIndex(b => b.ServerIdString);
        }

        /// <summary>
        /// A channel-based binding for badwords.
        /// </summary>
        public class BadWordChannelBinding
        {
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

            /// <summary>
            /// Gets or sets the server ID string.
            /// </summary>
            public string ServerIdString { get; set; }

            /// <summary>
            /// Gets or sets the server ID, backed by <see cref="ServerIdString"/>.
            /// </summary>
            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            /// <summary>
            /// Gets or sets the <see cref="List{T}"/> of channel-based badwords.
            /// </summary>
            public List<ChannelBadWord> BadWords { get; set; }
        }

        /// <summary>
        /// A guild-based binding for badwords.
        /// </summary>
        public class BadWordServerBinding
        {
            [Key]
            /// <summary>
            /// Gets or sets the server ID string.
            /// </summary>
            public string ServerIdString { get; set; }

            /// <summary>
            /// Gets or sets the server ID, backed by <see cref="ServerIdString"/>.
            /// </summary>
            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            /// <summary>
            /// Gets or sets the <see cref="List{T}"/> of guild-based badwords.
            /// </summary>
            public List<ServerBadWord> BadWords { get; set; }
        }

        /// <summary>
        /// A badword, filtered in a specific guild.
        /// </summary>
        public class ServerBadWord : IEquatable<ServerBadWord>
        {
            /// <summary>
            /// Gets or sets the server.
            /// </summary>
            public BadWordServerBinding Server { get; set; }

            [ForeignKey("Server")]
            /// <summary>
            /// Gets or sets the server ID string.
            /// </summary>
            public string ServerIdString { get; set; }

            /// <summary>
            /// Gets or sets the server ID, backed by <see cref="ServerIdString"/>.
            /// </summary>
            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            /// <summary>
            /// Gets or sets the string value of the badword.
            /// </summary>
            public string Entry { get; set; }

            public static bool operator ==(ServerBadWord left, ServerBadWord right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(ServerBadWord left, ServerBadWord right)
            {
                return !Equals(left, right);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((ServerIdString != null ? ServerIdString.GetHashCode() : 0) * 397) ^ (Entry != null ? Entry.GetHashCode() : 0);
                }
            }

            /// <inheritdoc />
            public bool Equals(ServerBadWord other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(ServerIdString, other.ServerIdString) && string.Equals(Entry, other.Entry);
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == this.GetType() && Equals((ServerBadWord)obj);
            }
        }

        /// <summary>
        /// A badword, filtered in a specific channel.
        /// </summary>
        public class ChannelBadWord : IEquatable<ChannelBadWord>
        {
            /// <summary>
            /// Gets or sets the channel.
            /// </summary>
            public BadWordChannelBinding Channel { get; set; }

            /// <summary>
            /// Gets or sets the channel ID string.
            /// </summary>
            [ForeignKey("Channel")]
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

            /// <summary>
            /// Gets or sets the server ID string.
            /// </summary>
            public string ServerIdString { get; set; }

            /// <summary>
            /// Gets or sets the server ID, backed by <see cref="ServerIdString"/>.
            /// </summary>
            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

            /// <summary>
            /// Gets or sets the string value of the badword.
            /// </summary>
            public string Entry { get; set; }

            public static bool operator ==(ChannelBadWord left, ChannelBadWord right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(ChannelBadWord left, ChannelBadWord right)
            {
                return !Equals(left, right);
            }

            /// <inheritdoc />
            public bool Equals(ChannelBadWord other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(ChannelIdString, other.ChannelIdString) && string.Equals(ServerIdString, other.ServerIdString) && string.Equals(Entry, other.Entry);
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == this.GetType() && Equals((ChannelBadWord)obj);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = ChannelIdString != null ? ChannelIdString.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (ServerIdString != null ? ServerIdString.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (Entry != null ? Entry.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }
    }
}
