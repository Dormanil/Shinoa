using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class Command : System.Attribute
    {
        public string CommandString { get; set; }
        public string[] Aliases { get; set; }
        
        public Command(string primaryCommandString, params string[] aliases)
        {
            this.CommandString = primaryCommandString;
            this.Aliases = aliases;
        }
    }
}
