// <copyright file="BlacklistService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Databases;
    using Discord;
    using Microsoft.EntityFrameworkCore;
    using static Databases.BlacklistUserContext;

    /// <summary>
    /// Service for adding and removing users from being able to use bot functions.
    /// </summary>
    public class BlacklistService : IDatabaseService
    {
        private DbContextOptions dbOptions;

        /// <inheritdoc cref="IService.Init"/>
        void IService.Init(dynamic config, IServiceProvider map)
        {
            dbOptions = map.GetService(typeof(DbContextOptions)) as DbContextOptions ?? throw new ServiceNotFoundException("Database Options were not found in service provider.");
        }

        /// <inheritdoc cref="IDatabaseService.RemoveBinding"/>
        async Task<bool> IDatabaseService.RemoveBinding(IEntity<ulong> guild)
        {
            using (var db = new BlacklistUserContext(dbOptions))
            {
                var entities = db.BlacklistUserBindings.Where(b => b.GuildId == guild.Id);
                if (!entities.Any()) return false;

                db.BlacklistUserBindings.RemoveRange(entities);
                await db.SaveChangesAsync();
                return true;
            }
        }

        /// <summary>
        /// Adds a binding to blacklist a user in a specific guild to not be able to use botfunctions.
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> AddBinding(IGuild guild, IGuildUser user)
        {
            using (var db = new BlacklistUserContext(dbOptions))
            {
                if (db.BlacklistUserBindings.Any(b => b.GuildId == guild.Id && b.UserId == user.Id)) return false;

                db.BlacklistUserBindings.Add(new BlacklistUserBinding
                {
                    GuildId = guild.Id,
                    UserId = user.Id,
                });
                await db.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> RemoveBinding(IGuild guild, IGuildUser user)
        {
            using (var db = new BlacklistUserContext(dbOptions))
            {
                var entity = await db.BlacklistUserBindings.FirstOrDefaultAsync(b => b.GuildId == guild.Id && b.UserId == user.Id);
                if (entity == null) return false;

                db.BlacklistUserBindings.Remove(entity);
                await db.SaveChangesAsync();
                return true;
            }
        }

        public bool CheckBinding(IGuild guild, IGuildUser user)
        {
            using (var db = new BlacklistUserContext(dbOptions))
            return db.BlacklistUserBindings.Any(b => b.GuildId == guild.Id && b.UserId == user.Id);
        }
    }
}