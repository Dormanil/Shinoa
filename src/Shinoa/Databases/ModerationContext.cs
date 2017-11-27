﻿// <copyright file="ModerationContext.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    using Discord;

    using Microsoft.EntityFrameworkCore;

    public class ModerationContext : DbContext, IDatabaseContext
    {
        /// <inheritdoc cref="DbContext" />
        public ModerationContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("moderation");

            modelBuilder.Entity<GuildUserMuteBinding>()
                .HasKey(b => new { b.GuildIdString, b.UserIdString });
        }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for guild to role bindings.
        /// </summary>
        public DbSet<GuildRoleBinding> GuildRoleBindings { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> for guild to user mute bindings.
        /// </summary>
        public DbSet<GuildUserMuteBinding> GuildUserMuteBindings { get; set; }

        public class GuildRoleBinding
        {
            [Key]
            public string GuildIdString { get; set; }

            public string RoleIdString { get; set; }

            [NotMapped]
            public ulong GuildId
            {
                get => ulong.Parse(GuildIdString);

                set => GuildIdString = value.ToString();
            }

            [NotMapped]
            public ulong RoleId
            {
                get => ulong.Parse(RoleIdString);

                set => RoleIdString = value.ToString();
            }

            [NotMapped]
            public IGuild Guild
            {
                get => Shinoa.Client.GetGuild(GuildId);

                set => GuildIdString = value.Id.ToString();
            }

            [NotMapped]
            public IRole Role
            {
                get => Guild?.GetRole(RoleId);

                set => RoleIdString = value.Id.ToString();
            }
        }

        public class GuildUserMuteBinding
        {
            public string GuildIdString { get; set; }

            public string UserIdString { get; set; }

            public DateTime? MuteTime { get; set; }

            [NotMapped]
            public ulong GuildId
            {
                get => ulong.Parse(GuildIdString);

                set => GuildIdString = value.ToString();
            }

            [NotMapped]
            public ulong UserId
            {
                get => ulong.Parse(UserIdString);

                set => UserIdString = value.ToString();
            }

            [NotMapped]
            public IGuild Guild
            {
                get => Shinoa.Client.GetGuild(GuildId);

                set => GuildIdString = value.Id.ToString();
            }

            [NotMapped]
            public IGuildUser User
            {
                get => Guild?.GetUserAsync(UserId).Result;

                set => UserIdString = value.Id.ToString();
            }
        }
    }
}