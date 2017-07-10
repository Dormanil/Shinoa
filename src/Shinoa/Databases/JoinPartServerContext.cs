// <copyright file="JoinPartServerContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// A <see cref="DbContext"/> for managing join and parting messages.
    /// </summary>
    public class JoinPartServerContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public JoinPartServerContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for join/part bindings.
        /// </summary>
        public DbSet<JoinPartServerBinding> JoinPartServerBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("joinpartserver");
        }

        /// <summary>
        /// Bindings to enable notifications of users joining/parting a guild.
        /// </summary>
        public class JoinPartServerBinding
        {
            /// <summary>
            /// Gets or sets the server ID string.
            /// </summary>
            [Key]
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
