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
            var guildId = guild.Id.ToString();
            return db.Table<BlacklistUserBinding>().Delete(b => b.GuildId == guildId) != 0;
        }

        public bool AddBinding(IGuild guild, IGuildUser user)
        {
            var guildId = guild.Id.ToString();
            var userId = user.Id.ToString();
            if (db.Table<BlacklistUserBinding>().Any(b => b.GuildId == guildId && b.UserId == userId)) return false;

            db.Insert(new BlacklistUserBinding
            {
                GuildId = guildId,
                UserId = userId,
            });
            return true;
        }

        public bool RemoveBinding(IGuild guild, IGuildUser user)
        {
            var guildId = guild.Id.ToString();
            var userId = user.Id.ToString();
            if (!db.Table<BlacklistUserBinding>().Any(b => b.GuildId == guildId && b.UserId == userId)) return false;

            db.Delete(new BlacklistUserBinding
            {
                GuildId = guildId,
                UserId = userId,
            });
            return true;
        }

        public bool CheckBinding(IGuild guild, IGuildUser user)
        {
            var guildId = guild.Id.ToString();
            var userId = user.Id.ToString();
            return db.Table<BlacklistUserBinding>().Any(b => b.GuildId == guildId && b.UserId == userId);
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