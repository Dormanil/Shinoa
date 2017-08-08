// <copyright file="ModerationService.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Services.TimedServices
{
    using System;
    using System.Threading.Tasks;

    using Discord;
    using Discord.WebSocket;

    public class ModerationService : ITimedService, IDatabaseService
    {
        /// <inheritdoc />
        public void Init(dynamic config, IServiceProvider map)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<bool> RemoveBinding(IEntity<ulong> binding)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task Callback()
        {
            throw new NotImplementedException();
        }

        public Task<BindingStatus> AddRole(SocketGuild guild, IRole mutedRole) => throw new NotImplementedException();

        public IRole GetRole(SocketGuild guild) => throw new NotImplementedException();

        public Task<BindingStatus> RemoveRole(SocketGuild guild, IRole role) => throw new NotImplementedException();

        public Task<BindingStatus> AddMute(SocketGuild guild, IGuildUser user, DateTime? until = null) => throw new NotImplementedException();

        public Task<BindingStatus> RemoveMute(SocketGuild guild, IGuildUser user) => throw new NotImplementedException();
    }
}
