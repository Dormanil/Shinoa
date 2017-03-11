using Discord;
using Discord.Commands;
using Shinoa.Attributes;
using System.Linq;
using System.Threading.Tasks;
using Shinoa.Services.TimedServices;

namespace Shinoa.Modules
{
    public class TwitterModule : ModuleBase<SocketCommandContext>
    {
        public enum TwitterOption
        {
            Add,
            Remove,
            List
        }

        private TwitterService service;

        public TwitterModule(TwitterService svc)
        {
            service = svc;
        }
        
        [Command("twitter"), RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task TwitterManagement(TwitterOption option, string user = "")
        {
            if (option != TwitterOption.List && user == "")
            {
                await ReplyAsync("Username required.");
                return;
            }

            var twitterName = user.Replace("@", "").ToLower().Trim();

            switch (option)
            {
                case TwitterOption.Add:
                {
                    if (service.AddBinding(twitterName, Context.Channel))
                    {
                        await ReplyAsync($"Notifications for @{twitterName} have been bound to this channel (#{Context.Channel.Name}).");
                    }
                    else
                    {
                        await ReplyAsync($"Notifications for @{twitterName} are already bound to this channel (#{Context.Channel.Name}).");
                    }
                }
                    break;
                case TwitterOption.Remove:
                {
                    if (service.RemoveBinding(twitterName, Context.Channel))
                    {
                        await ReplyAsync($"Notifications for @{twitterName} have been unbound from this channel (#{Context.Channel.Name}).");
                    }
                    else
                    {
                        await ReplyAsync($"Notifications for @{twitterName} are not currently bound to this channel (#{Context.Channel.Name}).");
                    }
                }
                    break;
                case TwitterOption.List:
                    var response = service.GetBindings(Context.Channel)
                        .Aggregate("", (current, binding) => current + $"@{binding.TwitterUsername}\n");

                    if (response == "") response = "N/A";

                    var embed = new EmbedBuilder()
                        .AddField(f => f.WithName("Twitter users currently bound to this channel").WithValue(response))
                        .WithColor(service.ModuleColor);

                    await ReplyAsync("", embed: embed.Build());
                    break;
            }
        }
    }
}
