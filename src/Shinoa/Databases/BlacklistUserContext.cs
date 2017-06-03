// <copyright file="BlacklistUserContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using Microsoft.EntityFrameworkCore;

    public class BlacklistUserContext : DbContext, IDatabaseContext
    {
        public BlacklistUserContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<BlacklistUserBinding> DbSet { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlacklistUserBinding>()
                .HasKey(b => new { b.GuildId, b.UserId });
            modelBuilder.HasDefaultSchema("blacklist");
        }

        public class BlacklistUserBinding
        {
            public ulong GuildId { get; set; }

            public ulong UserId { get; set; }
        }
    }
}
