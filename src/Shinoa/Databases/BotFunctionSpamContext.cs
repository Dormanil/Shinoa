// <copyright file="BotFunctionSpamContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    public class BotFunctionSpamContext : DbContext, IDatabaseContext
    {
        public BotFunctionSpamContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("botfunctionspam");
        }

        public DbSet<BotFunctionSpamBinding> DbSet { get; set; }

        public class BotFunctionSpamBinding
        {
            [Key]
            public ulong ChannelId { get; set; }
        }
    }
}
