// <copyright file="AnimeFeedContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.ComponentModel.DataAnnotations;

    using Microsoft.EntityFrameworkCore;

    public class AnimeFeedContext : DbContext, IDatabaseContext
    {
        public AnimeFeedContext(DbContextOptions dbContextOptions)
            : base(dbContextOptions)
        {
        }

        public DbSet<AnimeFeedBinding> DbSet { get; set; }

        public class AnimeFeedBinding
        {
            [Key]
            public ulong ChannelId { get; set; }

            public DateTime LatestPost { get; set; }
        }
    }
}
