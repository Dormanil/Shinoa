using Discord;
using Discord.Commands;
using Shinoa.Attributes;
using System.Linq;
using System.Threading.Tasks;
using Shinoa.Services.TimedServices;

namespace Shinoa.Modules
{
    [Group("twitter")]
    public class TwitterModule : ModuleBase<SocketCommandContext>
    {
        public enum TwitterOption
        {
            Add,
            Remove,
            List
        }

        public TwitterService service;

        public TwitterModule(TwitterService svc)
        {
            service = svc;
        }

        [Command("add"), RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Add(string user)
        {
            var twitterName = user.Replace("@", "").ToLower().Trim();
            if (service.AddBinding(twitterName, Context.Channel))
            {
                await ReplyAsync($"Notifications for @{twitterName} have been bound to this channel (#{Context.Channel.Name}).");
            }
            else
            {
                await ReplyAsync($"Notifications for @{twitterName} are already bound to this channel (#{Context.Channel.Name}).");
            }
        }

        [Command("remove"), RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task Remove(string user)
        {
            var twitterName = user.Replace("@", "").ToLower().Trim();
            if (service.RemoveBinding(twitterName, Context.Channel))
            {
                await ReplyAsync($"Notifications for @{twitterName} have been unbound from this channel (#{Context.Channel.Name}).");
            }
            else
            {
                await ReplyAsync($"Notifications for @{twitterName} are not currently bound to this channel (#{Context.Channel.Name}).");
            }
        }

        [Command("list")]
        public async Task List()
        {
            var response = service.GetBindings(Context.Channel)
                        .Aggregate("", (current, binding) => current + $"@{binding.TwitterUsername}\n");

            if (response == "") response = "N/A";

            var embed = new EmbedBuilder()
                .AddField(f => f.WithName("Twitter users currently bound to this channel").WithValue(response))
                .WithColor(service.ModuleColor);

            await ReplyAsync("", embed: embed.Build());
        }
    }
}
