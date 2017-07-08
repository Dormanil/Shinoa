// <copyright file="ServiceNotFoundException.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Databases
{
    using System;
    using System.Runtime.Serialization;

    public class ServiceNotFoundException : Exception
    {
        public ServiceNotFoundException()
            : base()
        {
        }

        public ServiceNotFoundException(string message)
            : base(message)
        {
        }

        public ServiceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ServiceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
