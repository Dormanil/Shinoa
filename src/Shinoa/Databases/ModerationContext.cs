// <copyright file="ModerationContext.cs" company="The Shinoa Development Team">
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
            internal ulong? GuildId
            {
                get => ulong.TryParse(GuildIdString, out ulong id) ? id : null as ulong?;

                set => GuildIdString = $"{value}";
            }

            [NotMapped]
            internal ulong? RoleId
            {
                get => ulong.TryParse(RoleIdString, out ulong id) ? id : null as ulong?;

                set => RoleIdString = $"{value}";
            }

            [NotMapped]
            internal IGuild Guild
            {
                get => Shinoa.Client.GetGuild(GuildId ?? 0ul);

                set => GuildIdString = $"{value.Id}";
            }

            [NotMapped]
            internal IRole Role
            {
                get => Guild?.GetRole(RoleId ?? 0ul);

                set => RoleIdString = $"{value.Id}";
            }
        }

        public class GuildUserMuteBinding
        {
            public string GuildIdString { get; set; }

            public string UserIdString { get; set; }

            public DateTime? MuteTime { get; set; }

            [NotMapped]
            internal ulong? GuildId
            {
                get => ulong.TryParse(GuildIdString, out ulong id) ? id : null as ulong?;

                set => GuildIdString = $"{value}";
            }

            [NotMapped]
            internal ulong? UserId
            {
                get => ulong.TryParse(UserIdString, out ulong id) ? id : null as ulong?;

                set => UserIdString = $"{value}";
            }

            [NotMapped]
            internal IGuild Guild
            {
                get => Shinoa.Client.GetGuild(GuildId ?? 0ul);

                set => GuildIdString = $"{value.Id}";
            }

            [NotMapped]
            internal IGuildUser User
            {
                get => Guild?.GetUserAsync(UserId ?? 0ul).Result;

                set => UserIdString = $"{value.Id}";
            }
        }
    }
}