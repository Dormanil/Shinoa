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

    public class BotFunctionSpamContext : DbContext, IDatabaseContext
    {
        public BotFunctionSpamContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<BotFunctionSpamBinding> BotFunctionSpamBindings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("botfunctionspam");
        }

        public class BotFunctionSpamBinding
        {
            [Key]
            public string ChannelIdString { get; set; }

            [NotMapped]
            public ulong ChannelId
            {
                get => ulong.Parse(ChannelIdString);
                set => ChannelIdString = value.ToString();
            }
        }
    }
}
