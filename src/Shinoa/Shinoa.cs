using Discord;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Shinoa
{
    public class Shinoa
    {
        public static DateTime StartTime = DateTime.Now;
        public static string Version = "2.0";

        public static dynamic Config;
        public static DiscordClient DiscordClient;
        public static SQLiteConnection DatabaseConnection = new SQLiteConnection("db.sqlite");

        public static Modules.Abstract.Module[] RunningModules =
        {            
            new Modules.BotAdministrationModule(),
            new Modules.LuaModule(),
            new Modules.HelpModule(),
            new Modules.ModerationModule(),
            new Modules.JoinPartModule(),
            new Modules.FunModule(),
            new Modules.MALAnimeModule(),
            new Modules.MALMangaModule(),
            new Modules.JapaneseDictModule(),
            new Modules.SAOWikiaModule(),
            new Modules.WikipediaModule(),
            new Modules.RedditModule(),
            new Modules.TwitterModule(),
            new Modules.AnimeFeedModule(),
            new Modules.SauceModule()
        };

        public static void Main(string[] args)
        {
            using (var streamReader = new StreamReader(new FileStream("config.yaml", FileMode.Open)))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                Shinoa.Config = deserializer.Deserialize(streamReader);
                Logging.Log("Config parsed and loaded.");
            }

            Console.Title = "Shinoa";
                        
            Shinoa.DiscordClient = new DiscordClient(x =>
            {
                x.AppName = "Shinoa";
            });

            Shinoa.DiscordClient.ExecuteAndWait(async () =>
            {
                while (true)
                {
                    try
                    {
                        await DiscordClient.Connect(Config["token"], TokenType.Bot);
                        break;
                    }
                    catch (Exception ex)
                    {
                        DiscordClient.Log.Error($"Login Failed", ex);
                        await Task.Delay(DiscordClient.Config.FailedReconnectDelay);
                    }
                }

                await Task.Delay(5000); // Not everything is instantly loaded if using a bot account.
                
                Logging.Log($"Logged into Discord as {DiscordClient.CurrentUser.Name}#{DiscordClient.CurrentUser.Discriminator}.");
                Logging.Log("---------------");

                DiscordClient.SetGame(Config["default_game"]);

                foreach(var module in RunningModules)
                {
                    Logging.Log($"Initializing module {module.GetType().Name}.");
                    module.Init();
                }

                Logging.Log($"All modules initialized successfully.");

                DiscordClient.MessageReceived += (s, e) =>
                {
                    if (e.Message.User.Id != DiscordClient.CurrentUser.Id)
                    {
                        if (e.Message.Channel.IsPrivate)
                        {
                            if (e.Message.User.Id != DiscordClient.CurrentUser.Id)
                            {
                                Logging.Log($"[PM] {e.User.Name}: {e.Message.Text}");
                            }
                        }

                        foreach (var module in RunningModules)
                        {
                            module.HandleMessage(s, e);

                            foreach (KeyValuePair<string, Modules.Abstract.Module.CommandFunction> commandDefinition in module.BoundCommands)
                            {
                                if (e.Message.Text.StartsWith(Config["command_prefix"] + commandDefinition.Key + " ") ||
                                    e.Message.Text.Trim() == Config["command_prefix"] + commandDefinition.Key)
                                {
                                    Logging.LogMessage(e.Message);

                                    try
                                    {
                                        commandDefinition.Value(e);
                                    }
                                    catch (Exception ex)
                                    {
                                        e.Channel.SendMessage("There was an error. Please check the command syntax and try again.");
                                        Logging.Log(ex.ToString());
                                    }

                                    return;
                                }
                            }
                        }
                    }
                };
            });
        }
    }
}
