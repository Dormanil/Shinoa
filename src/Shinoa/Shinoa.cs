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
            Map.Add(DiscordClient);
            Map.Add(CService);

            var modules = await CService.AddModulesAsync(typeof(Shinoa).GetTypeInfo().Assembly);

            foreach(var module in modules)
            {
                Logging.Log($"Loaded module \"{module.Name}\"");
                foreach (var command in module.Commands) Logging.Log($"Loaded command \"{command.Name}\"");
            }
            Logging.Log($"Loaded {CService.Modules.Count()} module(s) with {CService.Commands.Count()} command(s)");
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
                if (userMessage == null 
                || userMessage.Author.Id == DiscordClient.CurrentUser.Id 
                || !userMessage.HasStringPrefix((string)Config["command_prefix"], ref argPos)) return;
                
                var contextSock = new SocketCommandContext(DiscordClient, userMessage);
                Logging.LogMessage(contextSock);
                var res = await CService.ExecuteAsync(contextSock, argPos, Map);
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
