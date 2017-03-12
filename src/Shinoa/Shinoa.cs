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
using Shinoa.Attributes;
using Shinoa.Services;
using Shinoa.Services.TimedServices;

namespace Shinoa
{
    public static class Shinoa
    {
        private static readonly bool Alpha = Assembly.GetEntryAssembly().Location.ToLower().Contains("alpha");

        public static DateTime StartTime = DateTime.Now;
        public const string Version = "2.5.1K";
        public static readonly string VersionString = $"Shinoa v{Version}, built by OmegaVesko, FallenWarrior2k & Kazumi";

        public static dynamic Config;
        public static readonly DiscordSocketClient Client = new DiscordSocketClient();
        private static readonly CommandService Commands = new CommandService();
        private static readonly OpaqueDependencyMap Map = new OpaqueDependencyMap();
        private static SQLiteConnection databaseConnection;
        private static Timer globalRefreshTimer;
        private static readonly List<Func<Task>> Callbacks = new List<Func<Task>>();

        public static void Main(string[] args) =>
            StartAsync().GetAwaiter().GetResult();

        private static async Task StartAsync()
        {
            #region Prerequisites
            databaseConnection = Alpha ? new SQLiteConnection("db_alpha.sqlite") : new SQLiteConnection("db.sqlite");

            var configurationFileStream = Alpha ? new FileStream("config_alpha.yaml", FileMode.Open) : new FileStream("config.yaml", FileMode.Open);

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            using (var streamReader = new StreamReader(configurationFileStream))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                Shinoa.Config = deserializer.Deserialize(streamReader);
                await Logging.Log("Config parsed and loaded.");
            }
            #endregion

            Console.Title = $"Shinoa v{Version}";

            if (Alpha) await Logging.Log("Running in Alpha configuration.");

            #region Modules
            Map.Add(Client);
            Map.Add(Commands);
            Map.Add(databaseConnection);

            #region Services

            var services =
                typeof(Shinoa).GetTypeInfo()
                    .Assembly.GetExportedTypes()
                    .Select(t => t.GetTypeInfo())
                    .Where(t => t.GetInterfaces().Contains(typeof(IService)) && !(t.IsAbstract || t.IsInterface));
            #endregion

            var modules = await Commands.AddModulesAsync(typeof(Shinoa).GetTypeInfo().Assembly);

            foreach(var module in modules)
            {
                await Logging.Log($"Loaded module \"{module.Name}\"");
                foreach (var command in module.Commands) await Logging.Log($"Loaded command \"{command.Name}\"");
            }
            await Logging.Log($"Loaded {Commands.Modules.Count()} module(s) with {Commands.Commands.Count()} command(s)");
            #endregion

            #region Event handlers
            Client.Connected += async () =>
            {
                await Logging.Log($"Connected to Discord as {Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator}.");
                await Client.SetGameAsync(Config["global"]["default_game"]);
                var loggingChannel = Client.GetChannel(ulong.Parse(Config["global"]["logging_channel_id"])) as IMessageChannel;
                await Logging.InitLoggingToChannel(loggingChannel);

                await Logging.Log($"All modules initialized successfully. Shinoa is up and running.");
            };
            Client.MessageReceived += async (message) =>
            {
                var userMessage = message as SocketUserMessage;
                var argPos = 0;
                if (userMessage == null 
                || userMessage.Author.Id == Client.CurrentUser.Id 
                || !userMessage.HasStringPrefix((string)Config["global"]["command_prefix"], ref argPos)) return;
                
                var contextSock = new SocketCommandContext(Client, userMessage);
                await Logging.LogMessage(contextSock);
                var res = await Commands.ExecuteAsync(contextSock, argPos, Map);
                if (res.IsSuccess) return;

                await Logging.LogError(res.ErrorReason);
                string responseMessage = string.Empty;
                switch (res.Error)
                {
                    case CommandError.UnknownCommand:
                        responseMessage = "Unknown Command.";
                        break;
                    case CommandError.ParseFailed:
                        responseMessage = "The argument did not meet the requirements.";
                        break;
                    case CommandError.BadArgCount:
                        responseMessage = "The argument count was incorrect.";
                        break;
                    case CommandError.UnmetPrecondition:
                        responseMessage = "You are not allowed to execute this command.";
                        break;
                }
                if (responseMessage != string.Empty)
                {
                    responseMessage += $"\n\nReason: ```\n{res.ErrorReason}```";
                    await contextSock.Channel.SendMessageAsync(responseMessage);
                }
            };
            Client.Ready += () =>
            {
                foreach (var service in services)
                {
                    var configAttr = service.GetCustomAttribute<ConfigAttribute>();
                    dynamic config = null;
                    try
                    {
                        config = configAttr?.ConfigName != null ? Config[configAttr.ConfigName] : null;
                    }
                    catch (Exception e)
                    {
                        Logging.LogError(e.ToString()).GetAwaiter().GetResult();
                    }
                    var instance = (IService) Activator.CreateInstance(service.UnderlyingSystemType);
                    instance.Init(config, Map);
                    if (instance is ITimedService timedService)
                    {
                        Callbacks.Add(timedService.Callback);
                        Logging.Log($"Service \"{service.Name}\" added to callbacks").GetAwaiter().GetResult();
                    }
                    Logging.Log($"Loaded service \"{service.Name}\"").GetAwaiter().GetResult();
                    Map.AddOpaque(instance);
                }

                globalRefreshTimer = new Timer(async (s) =>
                {
                    foreach (var callback in Callbacks)
                    {
                        try
                        {
                            await callback();
                        }
                        catch (Exception e)
                        {
                            Logging.LogError(e.ToString()).GetAwaiter().GetResult();
                        }
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(int.Parse((string) Config["global"]["refresh_rate"])));
                return Task.CompletedTask;
            };
            #endregion

            #region Connection establishment
            await Logging.Log("Connecting to Discord...");
            await Client.LoginAsync(TokenType.Bot, Config["global"]["token"]);
            await Client.StartAsync();
            await Client.WaitForGuildsAsync();
            await Task.Delay(-1); 
            #endregion
        }
    }
}
