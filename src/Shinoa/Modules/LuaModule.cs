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
            this.BoundCommands.Add("lua", (e) =>
            {
                if (e.User.Id == ulong.Parse(Shinoa.Config["owner_id"]))
                {
                    var message = e.Channel.SendMessage($"Running...").Result;

                    var code = GetCommandParametersAsString(e.Message.Text);
                    var output = Script.RunString(code).ToString();

                    message.Edit($"Output: `{output}`");
                }
            });
        }
    }
}
