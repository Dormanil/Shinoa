using BoxKite.Twitter.Authentication;
using Discord;
using Discord.Commands;
using HtmlAgilityPack;
using Shinoa.Attributes;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shinoa.Modules
{
    public class TwitterModule : Abstract.UpdateLoopModule
    {
        static Color MODULE_COLOR = new Color(33, 150, 243);

        class Tweet
        {
            public long id;
            public string username;
            public string displayName;
            public bool isRetweet;
            public string content;
            public string avatarUrl;

            public Embed GetEmbed()
            {
                var embed = new EmbedBuilder()
                    .WithUrl($"https://twitter.com/{username}/status/{id}")
                    .WithDescription(System.Net.WebUtility.HtmlDecode(content))
                    .WithThumbnailUrl(avatarUrl)
                    .WithColor(MODULE_COLOR);

                if (isRetweet)
                    embed.Title = $"{displayName} (retweeted by @{username})";
                else
                    embed.Title = $"{displayName} (@{username})";

                return embed.Build();
            }
        }

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

            foreach (var boundUser in Shinoa.DatabaseConnection.Table<TwitterBinding>())
            {
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
        }


        [@Command("twitter")]
        public void TwitterManagement(CommandContext c, params string[] args)
        {
            if (c.Channel is IPrivateChannel || (c.User as IGuildUser).GuildPermissions.ManageGuild)
            {
                var channelIdString = c.Channel.Id.ToString();

                if (args[0] == "add")
                {
                    var twitterName = args[1].Replace("@", "").ToLower().Trim();

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
                else if (args[0] == "remove")
                {
                    var twitterName = args[1].Replace("@", "").ToLower().Trim();

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
                else if (args[0] == "list")
                {
                    var response = "";
                    foreach (var binding in Shinoa.DatabaseConnection.Table<TwitterBinding>()
                        .Where(item => item.ChannelId == channelIdString))
                    {
                        response += $"@{binding.TwitterUserName}\n";
                    }

                    if (response == "") response = "N/A";

                    var embed = new EmbedBuilder()
                        .AddField(f => f.WithName("Twitter users currently bound to this channel").WithValue(response))
                        .WithColor(MODULE_COLOR);

                    c.Channel.SendEmbedAsync(embed.Build());
                }
            }
            else
            {
                c.Channel.SendPermissionErrorAsync("Manage Server");
            }
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
                var newestTweet = GetNewestTweet(user.username);

                if (newestTweet != null)
                {
                    if (user.lastTweetIds.Count == 0)
                    {
                        user.lastTweetIds.Enqueue(newestTweet.id);
                    }
                    else if (!user.lastTweetIds.Contains(newestTweet.id))
                    {
                        if (newestTweet.isRetweet && !user.retweetsEnabled) continue;

                        user.lastTweetIds.Enqueue(newestTweet.id);
                        foreach (var channel in user.channels)
                        {
                            await channel.SendEmbedAsync(newestTweet.GetEmbed());
                        }
                    }
                }

                while (user.lastTweetIds.Count > 5) user.lastTweetIds.Dequeue();
            }
        }

        Tweet GetNewestTweet(string username)
        {
            try
            {
                var client = new HttpClient();
                var pageHtml = client.HttpGet($"https://mobile.twitter.com/{username}");
                var document = new HtmlDocument();
                document.LoadHtml(pageHtml);

                var latestTweetNode = document.DocumentNode.SelectNodes("//table[@class='tweet  ']").First();
                var tweet = new Tweet();
                tweet.isRetweet = !latestTweetNode.Attributes["href"].Value.ToLower().Contains(username.ToLower());
                tweet.id = long.Parse(latestTweetNode.SelectNodes("//div[@class='tweet-text']").First().Attributes["data-id"].Value);
                tweet.username = latestTweetNode.SelectNodes("//div[@class='username']").First().InnerText.Replace("@", "").Trim();
                tweet.displayName = latestTweetNode.SelectNodes("//strong[@class='fullname']").First().InnerText.Trim();
                tweet.content = latestTweetNode.SelectNodes("//div[@class='tweet-text']/div").First().InnerText.Trim();
                tweet.avatarUrl = latestTweetNode.SelectNodes("//td[@class='avatar']/a/img").First().Attributes["src"].Value;
                return tweet;
            }
            catch(Exception e)
            {
                Logging.Log(e.ToString());
                return null;
            }
        }
    }
}
