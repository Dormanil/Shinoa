using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using Discord.Commands;
using Shinoa.Attributes;

namespace Shinoa.Modules
{
    public class LuaModule : Abstract.Module
    {
        [@Command("lua", "run", "eval", "exec")]
        public void RunLua(CommandContext c, params string[] args)
        {
            if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
            {
                var message = c.Channel.SendMessageAsync($"Running...").Result;

                var code = args.ToRemainderString();
                var output = Script.RunString(code).ToString();

                message.ModifyAsync(p => p.Content = $"Output: `{output}`");
            }
        }
    }
}
