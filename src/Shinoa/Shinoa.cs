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
        public static DiscordSocketClient DiscordClient = new DiscordSocketClient();
        public static CommandService CService = new CommandService();
        public static DependencyMap Map = new DependencyMap();
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
            Start().GetAwaiter().GetResult();

        public static async Task Start()
        {
            #region Prerequisites
            Shinoa.DatabaseConnection = ALPHA ? new SQLiteConnection("db_alpha.sqlite") : new SQLiteConnection("db.sqlite");

            var configurationFileStream = ALPHA ? new FileStream("config_alpha.yaml", FileMode.Open) : new FileStream("config.yaml", FileMode.Open);

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            using (var streamReader = new StreamReader(configurationFileStream))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                Shinoa.Config = deserializer.Deserialize(streamReader);
                Logging.Log("Config parsed and loaded.");
            } 
            #endregion

            Console.Title = $"Shinoa v{Version}";

            if (ALPHA) Logging.Log("Running in Alpha configuration.");

            #region Modules
            foreach (var module in RunningModules)
            {
                Logging.Log($"Initializing module {module.GetType().Name}.");
                module.Init();

                foreach (var method in module.GetType().GetTypeInfo().DeclaredMethods)
                {
                    var commandAttribute = method.GetCustomAttribute<Attributes.Command>();
                    if (commandAttribute == null) continue;
                    Logging.Log($"Found command: '{commandAttribute.CommandString}'");

                    var definition = new CommandDefinition();
                    definition.commandStrings.Add(commandAttribute.CommandString);
                    definition.commandStrings.AddRange(commandAttribute.Aliases);
                    definition.methodInfo = method;
                    definition.moduleInstance = module;
                    Commands.Add(definition);
                }

                if (!(module is Modules.Abstract.UpdateLoopModule)) continue;
                Logging.Log($"Initializing update loop for module {module.GetType().Name}.");
                (module as Modules.Abstract.UpdateLoopModule).InitUpdateLoop();
            }

            Logging.Log("All modules initialized successfully.");


            //Begin Rework
            Map.Add(DiscordClient);
            Map.Add(CService);
            await CService.AddModulesAsync(typeof(Shinoa).GetTypeInfo().Assembly);
            //End Rework
            #endregion

            #region Event handlers
            DiscordClient.Connected += async () =>
               {
                   Logging.Log($"Connected to Discord as {DiscordClient.CurrentUser.Username}#{DiscordClient.CurrentUser.Discriminator}.");
                   await DiscordClient.SetGameAsync(Config["default_game"]);
               };
            DiscordClient.MessageReceived += async (message) =>
            {
                var userMessage = message as SocketUserMessage;
                int argPos = 0;
                if (userMessage == null || userMessage.Author.Id == DiscordClient.CurrentUser.Id || !userMessage.HasStringPrefix((string)Config["command_prefix"], ref argPos)) return;

                var context = new CommandContext(DiscordClient, userMessage);
                
                foreach (var moduleInstance in RunningModules)
                {
                    moduleInstance.HandleMessage(context);
                }

                foreach (var command in Commands)
                {
                    var splitMessage = context.Message.Content.Split(' ').ToList();
                    if (!command.commandStrings.Contains(splitMessage[0].Replace(Config["command_prefix"], "")))
                        continue;
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

                //Begin Rework
                var contextSock = new SocketCommandContext(DiscordClient, userMessage);
                var res = await CService.ExecuteAsync(contextSock, argPos, Map);
                if (!res.IsSuccess && res.Error != CommandError.UnknownCommand) Logging.Log(res.ErrorReason);
                //End Rework
            };
            #endregion

            #region Connection establishment
            Logging.Log("Connecting to Discord...");
            await DiscordClient.LoginAsync(TokenType.Bot, Config["token"]);
            await DiscordClient.StartAsync();
            await DiscordClient.WaitForGuildsAsync();
            await Task.Delay(-1); 
            #endregion
        }
    }
}
