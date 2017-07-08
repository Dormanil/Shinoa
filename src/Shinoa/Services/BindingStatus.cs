// <copyright file="BindingStatus.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    public enum BindingStatus
    {
        Error,
        AlreadyExists,
        NotExisting = AlreadyExists,
        Added,
        Removed = Added,
    }
}
