using Discord;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tweetinvi;

namespace Shinoa.Modules
{
    public class TwitterModule : Abstract.UpdateLoopModule
    {
        int UPDATE_INTERVAL = 20 * 1000;

        class TwitterBinding
        {
            [PrimaryKey, AutoIncrement]
            public int Id { get; set; }

            public string ChannelId { get; set; }
            public string TwitterUserName { get; set; }
        }

        class SubscribedUser
        {
            public string username; 
            public List<ITextChannel> channels = new List<ITextChannel>();
            public Queue<long> lastTweetIds = new Queue<long>();
            public bool retweetsEnabled;
        }

        List<SubscribedUser> SubscribedUsers = new List<SubscribedUser>();

        public override void Init()
        {
            Shinoa.DatabaseConnection.CreateTable<TwitterBinding>();
            
            var appCreds = Auth.SetApplicationOnlyCredentials(Shinoa.Config["twitter_key"], Shinoa.Config["twitter_secret"], true);
            Auth.InitializeApplicationOnlyCredentials(appCreds);

            foreach (var boundUser in Shinoa.DatabaseConnection.Table<TwitterBinding>())
            {
                //var channel = Shinoa.DiscordClient.GetChannel(ulong.Parse(boundUser.ChannelId));
                //var channelName = channel.IsPrivate ? channel.Name : "#" + channel.Name;
                //var serverName = channel.IsPrivate ? "[PM]" : channel.Server.Name;

                //Logging.Log($"  @{boundUser.TwitterUserName} -> [{serverName} -> {channelName}]");

                var channel = Shinoa.DiscordClient.GetChannel(ulong.Parse(boundUser.ChannelId));

                if (channel is IPrivateChannel)
                {
                    var privateChannel = channel as IPrivateChannel;
                    var channelName = privateChannel.Name;

                    Logging.Log($"  @{boundUser.TwitterUserName} -> [[PM] -> {channelName}]");
                }
                else if (channel is IGuildChannel)
                {
                    var guildChannel = channel as IGuildChannel;
                    var channelName = guildChannel.Name;
                    var guildName = guildChannel.Guild.Name;

                    Logging.Log($"  @{boundUser.TwitterUserName} -> [{guildName} -> #{channelName}]");
                }
            }

            this.BoundCommands.Add("twitter", (c) =>
            {
                if (c.Channel is IPrivateChannel || (c.User as IGuildUser).GuildPermissions.ManageGuild)
                {
                    var channelIdString = c.Channel.Id.ToString();

                    if (GetCommandParameters(c.Message.Content)[0] == "add")
                    {
                        var twitterName = GetCommandParameters(c.Message.Content)[1].Replace("@", "").ToLower().Trim();

                        if (Shinoa.DatabaseConnection.Table<TwitterBinding>()
                            .Where(item => item.ChannelId == channelIdString && item.TwitterUserName == twitterName).Count() == 0)
                        {
                            Shinoa.DatabaseConnection.Insert(new TwitterBinding()
                            {
                                ChannelId = c.Channel.Id.ToString(),
                                TwitterUserName = twitterName
                            });

                            c.Channel.SendMessageAsync($"Notifications for @{twitterName} have been bound to this channel (#{c.Channel.Name}).");
                        }
                        else
                        {
                            c.Channel.SendMessageAsync($"Notifications for @{twitterName} are already bound to this channel (#{c.Channel.Name}).");
                        }
                    }
                    else if (GetCommandParameters(c.Message.Content)[0] == "remove")
                    {
                        var twitterName = GetCommandParameters(c.Message.Content)[1].Replace("@", "").ToLower().Trim();

                        if (Shinoa.DatabaseConnection.Table<TwitterBinding>()
                            .Where(item => item.ChannelId == channelIdString && item.TwitterUserName == twitterName).Count() == 1)
                        {
                            var currentEntry = Shinoa.DatabaseConnection.Table<TwitterBinding>()
                                .Where(item => item.ChannelId == channelIdString && item.TwitterUserName == twitterName).First();

                            Shinoa.DatabaseConnection.Delete(new TwitterBinding() { Id = currentEntry.Id });

                            c.Channel.SendMessageAsync($"Notifications for @{twitterName} have been unbound from this channel (#{c.Channel.Name}).");
                        }
                        else
                        {
                            c.Channel.SendMessageAsync($"Notifications for @{twitterName} are not currently bound to this channel (#{c.Channel.Name}).");
                        }
                    }
                    else if (GetCommandParameters(c.Message.Content)[0] == "list")
                    {
                        var response = "Users currently bound to this channel:\n";
                        foreach (var binding in Shinoa.DatabaseConnection.Table<TwitterBinding>()
                            .Where(item => item.ChannelId == channelIdString))
                        {
                            response += $"- @{binding.TwitterUserName}\n";
                        }

                        c.Channel.SendMessageAsync(response.Trim());
                    }
                }
                else
                {
                    c.Channel.SendPermissionErrorAsync("Manage Server");
                }
            });
        }

        public async override Task UpdateLoop()
        {
            foreach (var user in SubscribedUsers) user.channels.Clear();
            foreach (var boundUser in Shinoa.DatabaseConnection.Table<TwitterBinding>())
            {
                if (SubscribedUsers.Any(item => item.username == boundUser.TwitterUserName))
                {
                    SubscribedUsers.Find(item => item.username == boundUser.TwitterUserName).channels.Add(
                        Shinoa.DiscordClient.GetChannel(ulong.Parse(boundUser.ChannelId)) as ITextChannel);
                }
                else
                {
                    var newUser = new SubscribedUser();
                    newUser.username = boundUser.TwitterUserName;
                    newUser.channels.Add(Shinoa.DiscordClient.GetChannel(ulong.Parse(boundUser.ChannelId)) as ITextChannel);
                    this.SubscribedUsers.Add(newUser);
                }
            }

            foreach (var user in SubscribedUsers)
            {
                var tweets = Timeline.GetUserTimeline(user.username);
                var newestTweet = tweets.ToList()[0];

                if (newestTweet.IsRetweet && !user.retweetsEnabled) continue;

                if (user.lastTweetIds.Count == 0)
                {
                    user.lastTweetIds.Enqueue(newestTweet.Id);
                }
                else if (!user.lastTweetIds.Contains(newestTweet.Id))
                {
                    user.lastTweetIds.Enqueue(newestTweet.Id);

                    var channelMessage = "";

                    channelMessage += $"New tweet from @{user.username} ({newestTweet.CreatedBy.Name}):\n\n";
                    channelMessage += $"```\n{newestTweet.FullText}\n```\n";
                    channelMessage += $"<{newestTweet.Url}>";

                    foreach (var channel in user.channels)
                    {
                        await channel.SendMessageAsync(channelMessage);
                    }
                }

                while (user.lastTweetIds.Count > 5) user.lastTweetIds.Dequeue();
            }
        }
    }
}
