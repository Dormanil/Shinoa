using System;
using System.Collections.Generic;
using System.Text;

namespace Shinoa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    class ConfigAttribute : Attribute
    {
        public string ConfigName { get; protected set; }

        public ConfigAttribute(string configName)
        {
            ConfigName = configName;
        }
    }
}
