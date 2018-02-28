// <copyright file="EntityEqualityComparer.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Extensions
{
    using System.Collections.Generic;
    using Discord;
    
    public class EntityEqualityComparer : IEqualityComparer<ISnowflakeEntity>
    {
        public bool Equals(ISnowflakeEntity left, ISnowflakeEntity right) => left.Id == right.Id;

        public int GetHashCode(ISnowflakeEntity obj) => (int)(obj.Id % int.MaxValue);
    }
}
