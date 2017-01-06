using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa
{
    public class Shinoa
    {
        public static DateTime StartTime = DateTime.Now;
        public static string Version = "2.1";
        public static string VersionString = $"Shinoa v{Version}, built by OmegaVesko";

        public static dynamic Config;
        public static DiscordSocketClient DiscordClient;
        public static SQLiteConnection DatabaseConnection = new SQLiteConnection("db.sqlite");

        static Timer GlobalUpdateTimer;    

        public static Modules.Abstract.Module[] RunningModules =
        {            
            new Modules.BotAdministrationModule(),
            new Modules.LuaModule(),
            new Modules.HelpModule(),
            new Modules.ModerationModule(),
            new Modules.JoinPartModule(),
            new Modules.FunModule(),
            new Modules.MALModule(),
            new Modules.JapaneseDictModule(),
            new Modules.SAOWikiaModule(),
            new Modules.WikipediaModule(),
            new Modules.SauceModule(),
            new Modules.RedditModule(),
            new Modules.TwitterModule(),
            new Modules.AnimeFeedModule()            
        };

        public static void Main(string[] args) =>
            new Shinoa().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            using (var streamReader = new StreamReader(new FileStream("config.yaml", FileMode.Open)))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                Shinoa.Config = deserializer.Deserialize(streamReader);
                Logging.Log("Config parsed and loaded.");
            }

            Console.Title = $"Shinoa v{Version}";

            Logging.Log("Connecting to Discord...");
            DiscordClient = new DiscordSocketClient();
            await DiscordClient.LoginAsync(TokenType.Bot, Config["token"]);
            await DiscordClient.ConnectAsync();
            Logging.Log($"Connected to Discord as {DiscordClient.CurrentUser.Username}#{DiscordClient.CurrentUser.Discriminator}.");

            await DiscordClient.SetGameAsync(Config["default_game"]);

            foreach (var module in RunningModules)
            {
                Logging.Log($"Initializing module {module.GetType().Name}.");
                module.Init();

                if (module is Modules.Abstract.UpdateLoopModule)
                {
                    Logging.Log($"Initializing update loop for module {module.GetType().Name}.");
                    (module as Modules.Abstract.UpdateLoopModule).InitUpdateLoop();
                }
            }

            Logging.Log($"All modules initialized successfully.");

            DiscordClient.MessageReceived+= async (message) =>
            {
                var userMessage = message as SocketUserMessage;
                if (userMessage == null) return; 

                var context = new CommandContext(DiscordClient, userMessage);

                if (context.User.Id != DiscordClient.CurrentUser.Id)
                {
                    if (context.IsPrivate)
                    {
                        Logging.Log($"[PM] {context.User.Username}: {context.Message.Content.ToString()}");
                    }

                    foreach (var module in RunningModules)
                    {
                        module.HandleMessage(context);

                        foreach (KeyValuePair<string, Modules.Abstract.Module.CommandFunction> commandDefinition in module.BoundCommands)
                        {
                            if (context.Message.Content.StartsWith(Config["command_prefix"] + commandDefinition.Key + " ") ||
                                context.Message.Content.Trim() == Config["command_prefix"] + commandDefinition.Key)
                            {
                                Logging.LogMessage(context);

                                try
                                {
                                    commandDefinition.Value(context);
                                }
                                catch (Exception ex)
                                {
                                    await context.Channel.SendMessageAsync("There was an error. Please check the command syntax and try again.");
                                    Logging.Log(ex.ToString());
                                }

                                return;
                            }
                        }
                    }
                }
            };

            await Task.Delay(-1);
        }
    }
}
