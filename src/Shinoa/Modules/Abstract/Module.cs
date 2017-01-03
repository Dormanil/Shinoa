using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa.Modules.Abstract
{
    public abstract class Module
    {
        public delegate void CommandFunction(MessageEventArgs e);
        public Dictionary<string, CommandFunction> BoundCommands = new Dictionary<string, CommandFunction>();

        protected static string[] GetCommandParameters(string message)
        {
            return message.Trim().Split(' ').Skip(1).ToArray();
        }

        protected static string GetCommandParametersAsString(string message)
        {
            var parameters = GetCommandParameters(message);
            var output = "";

            foreach (var word in parameters)
            {
                output += word + " ";
            }

            output = output.Trim();

            return output;
        }

        public virtual void Init()
        {
            return;
        }

        public virtual void HandleMessage(object sender, MessageEventArgs e)
        {
            return;
        }

        public virtual string DetailedStats { get { return null; } }
    }
}
