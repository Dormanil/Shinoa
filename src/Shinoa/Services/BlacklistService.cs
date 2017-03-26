// <copyright file="BlacklistService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System.Linq;
    using Discord;
    using Discord.Commands;
    using SQLite;

    public class BlacklistService : IDatabaseService
    {
        private SQLiteConnection db;

        public void Init(dynamic config, IDependencyMap map)
        {
            if (!map.TryGet(out db)) db = new SQLiteConnection(config["db_path"]);
            db.CreateTable<BlacklistUserBinding>();
        }

        public bool RemoveBinding(IEntity<ulong> guild)
        {
            return db.Table<BlacklistUserBinding>().Delete(b => b.GuildId == guild.Id.ToString()) != 0;
        }

        public bool AddBinding(IGuild guild, IGuildUser user)
        {
            if (db.Table<BlacklistUserBinding>().Any(b => b.GuildId == guild.Id.ToString() && b.UserId == user.Id.ToString())) return false;

            db.Insert(new BlacklistUserBinding
            {
                GuildId = guild.Id.ToString(),
                UserId = user.Id.ToString(),
            });
            return true;
        }

        public bool RemoveBinding(IGuild guild, IGuildUser user)
        {
            if (!db.Table<BlacklistUserBinding>().Any(b => b.GuildId == guild.Id.ToString() && b.UserId == user.Id.ToString())) return false;

            db.Delete(new BlacklistUserBinding
            {
                GuildId = guild.Id.ToString(),
                UserId = user.Id.ToString(),
            });
            return true;
        }

        public bool CheckBinding(IGuild guild, IGuildUser user)
        {
            return db.Table<BlacklistUserBinding>()
                .Any(b => b.GuildId == guild.Id.ToString() && b.UserId == user.Id.ToString());
        }

        public class BlacklistUserBinding
        {
            [Indexed]
            public string GuildId { get; set; }

            [Indexed]
            public string UserId { get; set; }
        }
    }
}