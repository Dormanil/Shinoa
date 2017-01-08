using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Shinoa
{
    public class CommandDefinition
    {
        public List<string> commandStrings = new List<string>();
        public MethodInfo methodInfo;
        public Modules.Abstract.Module moduleInstance;
    }
}
