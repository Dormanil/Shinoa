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
    public class Shinoa
    {
        public static bool ALPHA = Assembly.GetEntryAssembly().Location.ToLower().Contains("alpha");

        public static DateTime StartTime = DateTime.Now;
        public static string Version = "2.2.1";
        public static string VersionString = $"Shinoa v{Version}, built by OmegaVesko";

        public static dynamic Config;
        static DiscordSocketClient client = new DiscordSocketClient();
        static CommandService commands = new CommandService();
        static OpaqueDependencyMap map = new OpaqueDependencyMap();
        static SQLiteConnection databaseConnection;
        static Timer globalRefreshTimer;
        static List<Func<Task>> callbacks = new List<Func<Task>>();

        public static void Main(string[] args) =>
            Start().GetAwaiter().GetResult();

        public static async Task Start()
        {
            #region Prerequisites
            databaseConnection = ALPHA ? new SQLiteConnection("db_alpha.sqlite") : new SQLiteConnection("db.sqlite");

            var configurationFileStream = ALPHA ? new FileStream("config_alpha.yaml", FileMode.Open) : new FileStream("config.yaml", FileMode.Open);

            Console.OutputEncoding = System.Text.Encoding.Unicode;

            using (var streamReader = new StreamReader(configurationFileStream))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                Config = deserializer.Deserialize(streamReader);
                Logging.Log("Config parsed and loaded.");
            }
            #endregion

            Console.Title = $"Shinoa v{Version}";

            if (ALPHA) Logging.Log("Running in Alpha configuration.");

            #region Modules
            map.Add(client);
            map.Add(commands);
            map.Add(databaseConnection);

            #region Services

            var services =
                typeof(Shinoa).GetTypeInfo()
                    .Assembly.GetExportedTypes()
                    .Select(t => t.GetTypeInfo())
                    .Where(t => t.GetInterfaces().Contains(typeof(IService)) && !(t.IsAbstract || t.IsInterface));
            #endregion

            var modules = await commands.AddModulesAsync(typeof(Shinoa).GetTypeInfo().Assembly);

            foreach(var module in modules)
            {
                Logging.Log($"Loaded module \"{module.Name}\"");
                foreach (var command in module.Commands) Logging.Log($"Loaded command \"{command.Name}\"");
            }
            Logging.Log($"Loaded {commands.Modules.Count()} module(s) with {commands.Commands.Count()} command(s)");
            #endregion

            #region Event handlers
            client.Connected += async () =>
            {
                Logging.Log($"Connected to Discord as {client.CurrentUser.Username}#{client.CurrentUser.Discriminator}.");
                await client.SetGameAsync(Config["global"]["default_game"]);
            };
            client.MessageReceived += async (message) =>
            {
                var userMessage = message as SocketUserMessage;
                int argPos = 0;
                if (userMessage == null 
                || userMessage.Author.Id == client.CurrentUser.Id 
                || !userMessage.HasStringPrefix((string)Config["global"]["command_prefix"], ref argPos)) return;
                
                var contextSock = new SocketCommandContext(client, userMessage);
                Logging.LogMessage(contextSock);
                var res = await commands.ExecuteAsync(contextSock, argPos, map);
                if (res.IsSuccess) return;

                Logging.Log(res.ErrorReason);
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
            client.Ready += () =>
            {
                foreach (var service in services)
                {
                    var configAttr = service.GetCustomAttribute<ConfigAttribute>();
                    dynamic config = null;
                    try
                    {
                        config = configAttr?.ConfigName != null ? Config[configAttr.ConfigName] : null;
                    }
                    catch(Exception) { }
                    var instance = (IService) Activator.CreateInstance(service.UnderlyingSystemType);
                    instance.Init(config, map);
                    if (instance is ITimedService timedService)
                    {
                        callbacks.Add(timedService.Callback);
                        Logging.Log($"Service \"{service.Name}\" added to callbacks");
                    }
                    Logging.Log($"Loaded service \"{service.Name}\"");
                    map.AddOpaque(instance);
                }

                globalRefreshTimer = new Timer(async (s) =>
                {
                    foreach (var callback in callbacks)
                    {
                        try
                        {
                            await callback();
                        }
                        catch (Exception e)
                        {
                            Logging.Log(e.ToString());
                        }
                    }
                }, null, TimeSpan.Zero, TimeSpan.FromSeconds(int.Parse((string) Config["global"]["refresh_rate"])));
                return Task.CompletedTask;
            };
            #endregion

            #region Connection establishment
            Logging.Log("Connecting to Discord...");
            await client.LoginAsync(TokenType.Bot, Config["global"]["token"]);
            await client.StartAsync();
            await client.WaitForGuildsAsync();
            await Task.Delay(-1); 
            #endregion
        }
    }
}
