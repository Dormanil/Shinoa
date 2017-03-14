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

    /// <summary>
    /// Module to access Lua capabilities.
    /// </summary>
    public class LuaModule : ModuleBase<SocketCommandContext>
    {
        /// <summary>
        /// Command to run Lua code.
        /// </summary>
        /// <param name="code">The code to interpret and run.</param>
        /// <returns></returns>
        [Command("lua")]
        [Alias("run", "eval", "exec")]
        [RequireOwner]
        public async Task RunLua([Remainder]string code)
        {
            var messageTask = ReplyAsync($"Running...");

            var output = Script.RunString(code).ToString();

            var message = await messageTask;
            await message.ModifyAsync(p => p.Content = $"Output: `{output}`");
        }
    }
}
