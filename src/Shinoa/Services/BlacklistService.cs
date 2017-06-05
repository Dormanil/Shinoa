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
    using System.Threading.Tasks;
    using Databases;
    using Discord;
    using static Databases.BlacklistUserContext;

    public class BlacklistService : IDatabaseService
    {
        private BlacklistUserContext db;

        public void Init(dynamic config, IServiceProvider map)
        {
            db = map.GetService(typeof(BlacklistUserContext)) as BlacklistUserContext ?? throw new ServiceNotFoundException("Database context was not found in service provider.");
        }

        public bool RemoveBinding(IEntity<ulong> guild)
        {
            var entities = db.BlacklistUserBindings.Where(b => b.GuildId == guild.Id);
            if (!entities.Any()) return false;

            db.BlacklistUserBindings.RemoveRange(entities);
            return true;
        }

        public bool AddBinding(IGuild guild, IGuildUser user)
        {
            if (db.BlacklistUserBindings.Any(b => b.GuildId == guild.Id && b.UserId == user.Id)) return false;

            db.Add(new BlacklistUserBinding
            {
                GuildId = guild.Id,
                UserId = user.Id,
            });
            return true;
        }

        public bool RemoveBinding(IGuild guild, IGuildUser user)
        {
            var entity = db.BlacklistUserBindings.FirstOrDefault(b => b.GuildId == guild.Id && b.UserId == user.Id);

            if (entity == null)
                return false;

            db.Remove(entity);
            return true;
        }

        public bool CheckBinding(IGuild guild, IGuildUser user)
        {
            return db.BlacklistUserBindings.Any(b => b.GuildId == guild.Id && b.UserId == user.Id);
        }

        Task IDatabaseService.Callback() => db.SaveChangesAsync();
    }
}