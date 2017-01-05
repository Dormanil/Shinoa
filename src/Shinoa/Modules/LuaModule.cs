using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoonSharp.Interpreter;

namespace Shinoa.Modules
{
    public class LuaModule : Abstract.Module
    {
        public override void Init()
        {
            this.BoundCommands.Add("lua", (c) =>
            {
                if (c.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
                {
                    var message = c.Channel.SendMessageAsync($"Running...").Result;

                    var code = GetCommandParametersAsString(c.Message.Content);
                    var output = Script.RunString(code).ToString();

                    message.ModifyAsync(p => p.Content = $"Output: `{output}`");
                }
            });
        }
    }
}
