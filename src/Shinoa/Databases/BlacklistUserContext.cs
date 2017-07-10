// <copyright file="BlacklistUserContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// A <see cref="DbContext"/> to blacklist users.
    /// </summary>
    public class BlacklistUserContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public BlacklistUserContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for user blacklist bindings.
        /// </summary>
        public DbSet<BlacklistUserBinding> BlacklistUserBindings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlacklistUserBinding>()
                .HasKey(b => new { b.GuildIdString, b.UserIdString });
            modelBuilder.HasDefaultSchema("blacklist");
        }

        /// <summary>
        /// Bindings to blacklist users.
        /// </summary>
        public class BlacklistUserBinding
        {
            /// <summary>
            /// Gets or sets the ID string of the guild.
            /// </summary>
            public string GuildIdString { get; set; }

            /// <summary>
            /// Gets or sets the guild ID, backed by the <see cref="GuildIdString"/> property.
            /// </summary>
            [NotMapped]
            public ulong GuildId
            {
                get => ulong.Parse(GuildIdString);
                set => GuildIdString = value.ToString();
            }

            /// <summary>
            /// Gets or sets the ID string of the blacklisted user.
            /// </summary>
            public string UserIdString { get; set; }

            /// <summary>
            /// Gets or sets the user ID, backed by the <see cref="UserIdString"/> property.
            /// </summary>
            [NotMapped]
            public ulong UserId
            {
                get => ulong.Parse(UserIdString);
                set => UserIdString = value.ToString();
            }
        }
    }
}
