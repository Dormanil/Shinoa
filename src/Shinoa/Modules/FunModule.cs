using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;

namespace Shinoa.Modules
{
    public class FunModule : Abstract.Module
    {
        public override void HandleMessage(object sender, MessageEventArgs e)
        {
            if (e.User.Id != Shinoa.DiscordClient.CurrentUser.Id)
            {
                if (e.Message.Text == @"o/")
                    e.Channel.SendMessage(@"\o");
                else if (e.Message.Text == @"\o")
                    e.Channel.SendMessage(@"o/");
                else if (e.Message.Text == @"/o/")
                    e.Channel.SendMessage(@"\o\");
                else if (e.Message.Text == @"\o\")
                    e.Channel.SendMessage(@"/o/");
            }
        }

        public override void Init()
        {
            this.BoundCommands.Add("pick", (e) =>
            {
                var choices = GetCommandParametersAsString(e.Message.Text).Split(new string[] { " or " }, StringSplitOptions.RemoveEmptyEntries);
                var choice = choices[new Random().Next(choices.Length)].Trim();
                e.Channel.SendMessage($"<@{e.User.Id}> I choose '{choice}'.");
            });
        }
    }
}
