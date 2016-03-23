using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace Shinoa.Net.Module
{
    class AdminModule : IModule
    {
        public void Init()
        {
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.Message.User.Id.ToString().Equals(ShinoaNet.Config["owner_id"]))
            {
                if (Convenience.ContainsBotMention(e.Message.RawText) || e.Message.Channel.IsPrivate)
                {
                    var cleanMessage = Convenience.RemoveMentions(e.Message.RawText).Trim().ToLower();

                    if (cleanMessage.Equals("stats"))
                    {
                        var computerName = Environment.MachineName;
                        var userName = Environment.UserName;
                        var uptime = (DateTime.Now - ShinoaNet.StartTime);
                        var uptimeString = $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes, {uptime.Seconds} seconds.";

                        var modulesString = "";
                        foreach (var module in ShinoaNet.ActiveModules)
                        {
                            modulesString += $" - `{module.GetType().Name}`\n";
                        }

                        e.Channel.SendMessage($"**Shinoa.Net v. {ShinoaNet.VersionId}**\nRunning as {userName} @ {computerName}\nUptime: {uptimeString}\n\nActive modules:\n{modulesString}\nhttps://github.com/omegavesko/Shinoa.Net");
                    }
                }
            }            
        }
    }
}
