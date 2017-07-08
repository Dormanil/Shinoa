// <copyright file="ITimedService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services.TimedServices
{
    using System.Threading.Tasks;

    public interface ITimedService : IService
    {
        Task Callback();
    }
}
