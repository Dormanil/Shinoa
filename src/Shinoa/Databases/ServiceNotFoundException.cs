// <copyright file="ServiceNotFoundException.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Exception that is supposed to be thrown when a service was not found in the DI.
    /// </summary>
    public class ServiceNotFoundException : Exception
    {
        /// <inheritdoc cref="Exception"/>
        public ServiceNotFoundException()
            : base()
        {
        }

        /// <inheritdoc cref="Exception"/>
        public ServiceNotFoundException(string message)
            : base(message)
        {
        }

        /// <inheritdoc cref="Exception"/>
        public ServiceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <inheritdoc cref="Exception"/>
        public ServiceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
