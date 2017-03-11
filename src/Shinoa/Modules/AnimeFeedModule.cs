using Discord;
using Discord.Commands;
using Shinoa.Attributes;
using System.Threading.Tasks;
using Shinoa.Services.TimedServices;

namespace Shinoa.Modules
{
    public class AnimeFeedModule : ModuleBase<SocketCommandContext>
    {
        public enum AnimeFeedOption
        {
            Enable,
            Disable
        }

        private AnimeFeedService service;

        public AnimeFeedModule(AnimeFeedService svc)
        {
            service = svc;
        }

        [Command("animefeed"), RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task AnimeFeedManagement(AnimeFeedOption option)
        {
            switch (option)
            {
                case AnimeFeedOption.Enable:

                    if (service.AddBinding(Context.Channel))
                    {

                        if (!(Context.Channel is IPrivateChannel))
                            await ReplyAsync($"Anime notifications have been bound to this channel (#{Context.Channel.Name}).");
                        else
                            await ReplyAsync("You will now receive anime notifications via PM.");
                    }
                    else
                    {
                        if (!(Context.Channel is IPrivateChannel))
                            await ReplyAsync($"Anime notifications are already bound to this channel (#{Context.Channel.Name}).");
                        else
                            await ReplyAsync("You are already receiving anime notifications.");
                    }
                    break;
                case AnimeFeedOption.Disable:
                    if (service.RemoveBinding(Context.Channel))
                    {
                        if (!(Context.Channel is IPrivateChannel))
                            await ReplyAsync($"Anime notifications have been unbound from this channel (#{Context.Channel.Name}).");
                        else
                            await ReplyAsync("You will no lonnger receive anime notifications.");
                    }
                    else
                    {
                        if (!(Context.Channel is IPrivateChannel))
                            await ReplyAsync($"Anime notifications are currently not bound to this channel (#{Context.Channel.Name}).");
                        else
                            await ReplyAsync("You are currently not receiving anime notifications.");
                    }
                    break;
            }
        }
    }
}
