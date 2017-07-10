// <copyright file="IService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services
{
    using System;

    /// <summary>
    /// Interface for common tasks every service should be doing.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Initiates the state of the service.
        /// </summary>
        /// <param name="config">Specialised configuration for the service.</param>
        /// <param name="map">ServiceProvider providing internals for the service.</param>
        void Init(dynamic config, IServiceProvider map);
    }
}
