using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Shinoa.Modules
{
    public class FunModule : Abstract.Module
    {
        public override void HandleMessage(CommandContext context)
        {
            if (context.User.Id != Shinoa.DiscordClient.CurrentUser.Id)
            {
                if (context.Message.Content == @"o/")
                    context.Channel.SendMessageAsync(@"\o");
                else if (context.Message.Content == @"\o")
                    context.Channel.SendMessageAsync(@"o/");
                else if (context.Message.Content == @"/o/")
                    context.Channel.SendMessageAsync(@"\o\");
                else if (context.Message.Content == @"\o\")
                    context.Channel.SendMessageAsync(@"/o/");
            }
        }

        public override void Init()
        {
            this.BoundCommands.Add("pick", (c) =>
            {
                var choices = GetCommandParametersAsString(c.Message.Content).Split(new string[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
                var choice = choices[new Random().Next(choices.Length)].Trim();
                c.Channel.SendMessageAsync($"<@{c.User.Id}> I choose '{choice}'.");
            });
        }
    }
}
