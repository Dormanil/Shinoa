// <copyright file="JoinPartServerContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using Microsoft.EntityFrameworkCore;

    public class JoinPartServerContext : DbContext, IDatabaseContext
    {
        public JoinPartServerContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<JoinPartServerBinding> JoinPartServerBindings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("joinpartserver");
        }

        public class JoinPartServerBinding
        {
            [Key]
            public string ServerIdString { get; set; }

            [NotMapped]
            public ulong ServerId
            {
                get => ulong.Parse(ServerIdString);
                set => ServerIdString = value.ToString();
            }

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
