using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Shinoa
{
    public class Shinoa
    {
        public static bool ALPHA = Assembly.GetEntryAssembly().Location.ToLower().Contains("alpha");

        public static DateTime StartTime = DateTime.Now;
        public static string Version = "2.2.1";
        public static string VersionString = $"Shinoa v{Version}, built by OmegaVesko";

        public static dynamic Config;
        public static DiscordSocketClient DiscordClient;
        public static SQLiteConnection DatabaseConnection;

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
            new Modules.AnilistModule(),
            new Modules.JapaneseDictModule(),
            new Modules.SAOWikiaModule(),
            new Modules.WikipediaModule(),
            new Modules.SauceModule(),
            new Modules.RedditModule(),
            new Modules.TwitterModule(),
            new Modules.AnimeFeedModule()
        };

        public static List<CommandDefinition> Commands = new List<CommandDefinition>();

        public static void Main(string[] args) =>
            new Shinoa().Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            if (ALPHA)
                Shinoa.DatabaseConnection = new SQLiteConnection("db_alpha.sqlite");
            else
                Shinoa.DatabaseConnection = new SQLiteConnection("db.sqlite");

            FileStream configurationFileStream;
            if (ALPHA)
                configurationFileStream = new FileStream("config_alpha.yaml", FileMode.Open);
            else
                configurationFileStream = new FileStream("config.yaml", FileMode.Open);

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            using (var streamReader = new StreamReader(configurationFileStream))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                Shinoa.Config = deserializer.Deserialize(streamReader);
                Logging.Log("Config parsed and loaded.");
            }

            Console.Title = $"Shinoa v{Version}";

            if (ALPHA) Logging.Log("Running in Alpha configuration.");

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

                foreach (var method in module.GetType().GetTypeInfo().DeclaredMethods)
                {
                    var commandAttribute = method.GetCustomAttribute<Attributes.Command>();
                    if (commandAttribute != null)
                    {
                        Logging.Log($"Found command: '{commandAttribute.CommandString}'");

                        var definition = new CommandDefinition();
                        definition.commandStrings.Add(commandAttribute.CommandString);
                        definition.commandStrings.AddRange(commandAttribute.Aliases);
                        definition.methodInfo = method;
                        definition.moduleInstance = module;
                        Commands.Add(definition);
                    }
                }

                if (module is Modules.Abstract.UpdateLoopModule)
                {
                    Logging.Log($"Initializing update loop for module {module.GetType().Name}.");
                    (module as Modules.Abstract.UpdateLoopModule).InitUpdateLoop();
                }
            }
            Logging.InitLoggingToChannel();

            Logging.Log($"All modules initialized successfully. Shinoa is up and running.");

            DiscordClient.MessageReceived += async (message) =>
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

                    foreach (var moduleInstance in RunningModules)
                    {
                        moduleInstance.HandleMessage(context);
                    }

                    foreach (var command in Commands)
                    {
                        var splitMessage = context.Message.Content.Split(' ').ToList();
                        if (message.Content.StartsWith(Config["command_prefix"]) && command.commandStrings.Contains(splitMessage[0].Replace(Config["command_prefix"], "")))
                        {
                            Logging.LogMessage(context);

                            splitMessage.RemoveAt(0);
                            var paramsObject = new object[] { context, splitMessage.ToArray() };

                            try
                            {
                                command.methodInfo.Invoke(command.moduleInstance, paramsObject);
                            }
                            catch (Exception ex)
                            {
                                await context.Channel.SendMessageAsync("There was an error. Please check the command syntax and try again.");
                                Logging.Log(ex.ToString());
                            }
                        }
                    }
                }
            };

            await Task.Delay(-1);
        }
    }
}
