// <copyright file="ConfigAttribute.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Attributes
{
    using System;

    /// <summary>
    /// Represents a shortcut for configuration items.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigAttribute"/> class.
        /// </summary>
        /// <param name="configName">The name of the configuration item.</param>
        public ConfigAttribute(string configName)
        {
            ConfigName = configName;
        }

        /// <summary>
        /// Gets or sets the name of the configuration item.
        /// </summary>
        public string ConfigName { get; protected set; }
    }
}
