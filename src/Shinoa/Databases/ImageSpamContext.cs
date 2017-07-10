// <copyright file="ImageSpamContext.cs" company="The Shinoa Development Team">
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
    /// A <see cref="DbContext"/> to limit image spam.
    /// </summary>
    public class ImageSpamContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public ImageSpamContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for image spam bindings.
        /// </summary>
        public DbSet<ImageSpamBinding> ImageSpamBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("imagespam");
        }

        /// <summary>
        /// Bindings to limit image spam.
        /// </summary>
        public class ImageSpamBinding
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
        }
    }
}
