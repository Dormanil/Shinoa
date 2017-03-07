using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoonSharp.Interpreter;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class LuaModule : ModuleBase<SocketCommandContext>
    {
        [Command("lua"), Alias("run", "eval", "exec"), RequireOwner]
        public async Task RunLua([Remainder]string code)
        {
            var messageTask = ReplyAsync($"Running...");
                
            var output = Script.RunString(code).ToString();

            var message = await messageTask;
            await message.ModifyAsync(p => p.Content = $"Output: `{output}`");
        }
    }
}
