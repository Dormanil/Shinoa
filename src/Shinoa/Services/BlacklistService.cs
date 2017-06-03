// <copyright file="BlacklistService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System;
    using System.Linq;
    using Databases;
    using Discord;
    using Discord.Commands;
    using static Databases.BlacklistUserContext;
    using System.Threading.Tasks;

    public class BlacklistService : IDatabaseService
    {
        private BlacklistUserContext db;

        public void Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(BlacklistUserContext)) as BlacklistUserContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");
        }

        public bool RemoveBinding(IEntity<ulong> guild)
        {
            var entities = db.DbSet.Where(b => b.GuildId == guild.Id);
            if (entities.Count() == 0) return false;

            db.RemoveRange(entities);
            db.SaveChanges();
            return true;
        }

        public bool AddBinding(IGuild guild, IGuildUser user)
        {
            if (db.DbSet.Any(b => b.GuildId == guild.Id && b.UserId == user.Id)) return false;

            db.Add(new BlacklistUserBinding
            {
                GuildId = guild.Id,
                UserId = user.Id,
            });
            db.SaveChanges();
            return true;
        }

        public bool RemoveBinding(IGuild guild, IGuildUser user)
        {
            var entity = db.DbSet.FirstOrDefault(b => b.GuildId == guild.Id && b.UserId == user.Id);

            if (entity == null)
                return false;

            db.Remove(entity);
            db.SaveChanges();
            return true;
        }

        public bool CheckBinding(IGuild guild, IGuildUser user)
        {
            return db.DbSet.Any(b => b.GuildId == guild.Id && b.UserId == user.Id);
        }

        Task IDatabaseService.Callback() => db.SaveChangesAsync();
    }
}