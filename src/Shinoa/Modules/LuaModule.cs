// <copyright file="LuaModule.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa.Modules
{
    using System.Threading.Tasks;
    using Discord.Commands;
    using MoonSharp.Interpreter;

    public class LuaModule : ModuleBase<SocketCommandContext>
    {
        [Command("lua")]
        [Alias("run", "eval", "exec")]
        [RequireOwner]
        public async Task RunLua([Remainder]string code)
        {
            var messageTask = this.ReplyAsync($"Running...");

            var output = Script.RunString(code).ToString();

            var message = await messageTask;
            await message.ModifyAsync(p => p.Content = $"Output: `{output}`");
        }
    }
}
