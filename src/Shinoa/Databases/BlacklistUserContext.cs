// <copyright file="BlacklistUserContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    public class BlacklistUserContext : DbContext, IDatabaseContext
    {
        public BlacklistUserContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<BlacklistUserBinding> BlacklistUserBindings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlacklistUserBinding>()
                .HasKey(b => new { b.GuildIdString, b.UserIdString });
            modelBuilder.HasDefaultSchema("blacklist");
        }

        public class BlacklistUserBinding
        {
            public string GuildIdString { get; set; }

            [NotMapped]
            public ulong GuildId
            {
                get => ulong.Parse(GuildIdString);
                set => GuildIdString = value.ToString();
            }

            public string UserIdString { get; set; }

            [NotMapped]
            public ulong UserId
            {
                get => ulong.Parse(UserIdString);
                set => UserIdString = value.ToString();
            }
        }
    }
}
