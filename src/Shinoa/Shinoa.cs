// <copyright file="Shinoa.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
// All rights reserved.
// Licensed under the MIT license.
// </copyright>

namespace Shinoa
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Attributes;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Services;
    using Services.TimedServices;
    using SQLite;
    using SQLite.Extensions;
    using YamlDotNet.Serialization;

    public static class Shinoa
    {
        public const string Version = "2.5.1K";
        public static readonly string VersionString = $"Shinoa v{Version}, built by OmegaVesko, FallenWarrior2k & Kazumi";
        public static CancellationTokenSource Cts = new CancellationTokenSource();
        private static readonly bool Alpha = Assembly.GetEntryAssembly().Location.ToLower().Contains("alpha");
        private static readonly CommandService Commands = new CommandService();
        private static readonly OpaqueDependencyMap Map = new OpaqueDependencyMap();
        private static readonly Dictionary<Type, Func<Task>> Callbacks = new Dictionary<Type, Func<Task>>();
        private static SQLiteConnection databaseConnection;
        private static Timer globalRefreshTimer;

        public static dynamic Config { get; private set; }

        public static DiscordSocketClient Client { get; } = new DiscordSocketClient( new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Error, // Can't do LogSeverity.Warning because the continuous ratelimit warnings cause a StackOverflow
        });

        public static DateTime StartTime { get; } = DateTime.Now;

        public static void Main(string[] args) =>
            StartAsync().GetAwaiter().GetResult();

        private static async Task StartAsync()
        {
            #region Prerequisites
            databaseConnection = Alpha ? new SQLiteConnection("db_alpha.sqlite") : new SQLiteConnection("db.sqlite");

            var configurationFileStream = Alpha ? new FileStream("config_alpha.yaml", FileMode.Open) : new FileStream("config.yaml", FileMode.Open);

            Console.OutputEncoding = Encoding.Unicode;

            using (var streamReader = new StreamReader(configurationFileStream))
            {
                var deserializer = new Deserializer();
                Config = deserializer.Deserialize(streamReader);
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

            foreach (var module in modules)
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
                try
                {
                    await Client.SetGameAsync((string) Config["global"]["default_game"]);
                }
                catch (KeyNotFoundException)
                {
                    await Logging.Log("The property was not found on the dynamic object. No default game was supplied.");
                }
            };
            Client.Disconnected += async (e) =>
            {
                Logging.StopLoggingToChannel();
                await Logging.Log("Disconnected from Discord.");
                if (e != null)
                {
                    await Logging.LogError(e.ToString());
                }
            };
            Client.Log += async msg =>
            {
                await Logging.Log($"{msg.Severity}: {msg.Message}");
                if (msg.Exception != null) await Logging.LogError($"{msg.Source}: {msg.Exception.ToString()}");
            };
            Client.GuildAvailable += async g =>
            {
                string loggingChannelIdString = null;
                try
                {
                    loggingChannelIdString = (string) Config["global"]["logging_channel_id"];
                }
                catch (KeyNotFoundException)
                {
                    await Logging.LogError(
                        "The property was not found on the dynamic object. No logging channel was supplied.");
                }
                catch (Exception e)
                {
                    await Logging.LogError(e.ToString());
                }

                if (loggingChannelIdString == null) return;
                if (ulong.TryParse(loggingChannelIdString, out ulong loggingChannelId))
                {
                    if (Client.GetChannel(loggingChannelId) is IMessageChannel loggingChannel)
                    {
                        await Logging.InitLoggingToChannel(loggingChannel);
                    }
                }
            };
            Client.MessageReceived += async message =>
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
                var responseMessage = string.Empty;
                switch (res.Error)
                {
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
            Client.Ready += async () =>
            {
                foreach (var service in services)
                {
                    var instance = (IService)Activator.CreateInstance(service.UnderlyingSystemType);
                    if (!Map.TryAddOpaque(instance)) continue;

                    var configAttr = service.GetCustomAttribute<ConfigAttribute>();
                    dynamic config = null;
                    try
                    {
                        config = configAttr?.ConfigName != null ? Config[configAttr.ConfigName] : null;
                    }
                    catch (KeyNotFoundException)
                    {
                        await Logging.LogError($"The property was not found on the dynamic object. No service settings for \"{service.Name}\" were supplied.");
                    }
                    catch (Exception e)
                    {
                        await Logging.LogError(e.ToString());
                    }

                    instance.Init(config, Map);

                    if (instance is ITimedService timedService)
                    {
                        Callbacks.TryAdd(service.UnderlyingSystemType, timedService.Callback);
                        await Logging.Log($"Service \"{service.Name}\" added to callbacks");
                    }

                    await Logging.Log($"Loaded service \"{service.Name}\"");
                }

                var refreshRate = 30;
                try
                {
                    refreshRate = int.Parse(Config["global"]["refresh_rate"]);
                }
                catch (KeyNotFoundException)
                {
                   await  Logging.LogError("The property was not found on the dynamic object. No global refresh rate was supplied. Defaulting to once every 30 seconds.");
                }
                catch (Exception e)
                {
                    await Logging.LogError(e.ToString());
                }

                globalRefreshTimer = new Timer(
                    async s =>
                    {
                        foreach (var callback in Callbacks.Values)
                        {
                            try
                            {
                                await callback();
                            }
                            catch (Exception e)
                            {
                                await Logging.LogError(e.ToString());
                            }
                        }
                    },
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(refreshRate));

                await Logging.Log("All modules initialized successfully. Shinoa is up and running.");
            };
            #endregion

            #region Connection establishment
            await Logging.Log("Connecting to Discord...");
            await Client.LoginAsync(TokenType.Bot, (string)Config["global"]["token"]);
            await Client.StartAsync();

            var completionSource = new TaskCompletionSource<object>();
            Cts.Token.Register(() => completionSource.TrySetCanceled());
            var blockTask = Task.Delay(-1, Cts.Token);
            await Task.WhenAny(blockTask, completionSource.Task);
            await Client.LogoutAsync();
            await Client.StopAsync();
            await Logging.Log("Exiting.");
            #endregion
        }
    }
}
