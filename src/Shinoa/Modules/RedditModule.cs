using Discord;
using Discord.Commands;
using Shinoa.Attributes;
using System.Linq;
using System.Threading.Tasks;
using Shinoa.Services;

namespace Shinoa.Modules
{
    //TODO: Finish migrate
    public class RedditModule : ModuleBase<SocketCommandContext>
    {
        public enum RedditOption
        {
            Add,
            Remove,
            List
        }

        private readonly RedditService service;

        public RedditModule(RedditService svc)
        {
            service = svc;
        }

        [Command("reddit"), RequireGuildUserPermission(GuildPermission.ManageGuild)]
        public async Task RedditManagement(RedditOption option, string subredditName = "")
        {
            subredditName = subredditName.Replace("r/", "").Replace("/", "");
            if (option != RedditOption.List && subredditName == "")
            {
                await ReplyAsync("Subreddit name required.");
                return;
            }

            switch (option)
            {
                case RedditOption.Add:
                {

                    if (service.AddBinding(subredditName, Context.Channel))
                    {
                        await ReplyAsync($"Notifications for /r/{subredditName} have been bound to this channel (#{Context.Channel.Name}).");
                    }
                    else
                    {
                        await ReplyAsync($"Notifications for /r/{subredditName} are already bound to this channel (#{Context.Channel.Name}).");
                    }
                }
                    break;
                case RedditOption.Remove:
                {

                    if (service.RemoveBinding(subredditName, Context.Channel))
                    {
                        await ReplyAsync($"Notifications for /r/{subredditName} have been unbound from this channel (#{Context.Channel.Name}).");
                    }
                    else
                    {
                        await ReplyAsync($"Notifications for /r/{subredditName} are not currently bound to this channel (#{Context.Channel.Name}).");
                    }
                }
                    break;
                case RedditOption.List:
                    var response = service.GetBindings(Context.Channel)
                        .Aggregate("", (current, binding) => current + $"/r/{binding.SubredditName}\n");

                    if (response == "") response = "N/A";

                    var embed = new EmbedBuilder()
                        .AddField(f => f.WithName("Subreddits currently bound to this channel").WithValue(response))
                        .WithColor(service.ModuleColor);

                    await ReplyAsync("", embed: embed.Build());
                    break;
            }
        }
    }
}