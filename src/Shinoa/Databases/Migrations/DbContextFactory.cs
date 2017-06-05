// <copyright file="DbContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using System;
    using System.IO;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;

    /// <summary>
    /// A factory for creating migrations.
    /// </summary>
    public abstract class DbContextFactory
    {
        protected DbContextFactory()
        {
            dynamic config;
            using (var configFstream = new FileStream("config.yaml", FileMode.Open))
            using (var configReader = new StreamReader(configFstream))
            {
                config = new YamlDotNet.Serialization.Deserializer().Deserialize(configReader);
            }

            DatabaseProvider dbProvider = Enum.Parse<DatabaseProvider>(config["global"]["database"]["provider"]);
            string connectionString = config["global"]["database"]["connect_string"];
            var optionsBuilder = new DbContextOptionsBuilder();
            switch (dbProvider)
            {
                case DatabaseProvider.SQLServer:
                    optionsBuilder.UseSqlServer(connectionString);
                    break;
                case DatabaseProvider.PostgreSQL:
                    optionsBuilder.UseNpgsql(connectionString);
                    break;
                default:
                    throw new NotSupportedException("The given database provider is not supported.");
            }

            options = optionsBuilder.Options;
        }

        protected readonly DbContextOptions options;
    }
}
