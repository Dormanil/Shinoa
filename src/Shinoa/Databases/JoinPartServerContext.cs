// <copyright file="JoinPartServerContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    public class JoinPartServerContext : DbContext, IDatabaseContext
    {
        public JoinPartServerContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<JoinPartServerBinding> DbSet { get; set; }

        public class JoinPartServerBinding
        {
            [Key]
            public ulong ServerId { get; set; }

            public ulong ChannelId { get; set; }
        }
    }
}
