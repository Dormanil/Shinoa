// <copyright file="ImageSpamContextFactory.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases.Migrations
{
    using Microsoft.EntityFrameworkCore.Infrastructure;

    public class ImageSpamContextFactory : DbContextFactory, IDbContextFactory<ImageSpamContext>
    {
        public ImageSpamContext Create(string[] args) => new ImageSpamContext(options);
    }
}