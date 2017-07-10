// <copyright file="BotFunctionSpamContext.cs" company="The Shinoa Development Team">
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
    /// A <see cref="DbContext"/> to limit botfunction spam.
    /// </summary>
    public class BotFunctionSpamContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public BotFunctionSpamContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for botfunction spam bindings.
        /// </summary>
        public DbSet<BotFunctionSpamBinding> BotFunctionSpamBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("botfunctionspam");
        }

        /// <summary>
        /// Bindings to limit botfunction spam.
        /// </summary>
        public class BotFunctionSpamBinding
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
