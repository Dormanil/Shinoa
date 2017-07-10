// <copyright file="Shinoa.cs" company="The Shinoa Development Team">
// Copyright (c) 2016 - 2017 OmegaVesko.
// Copyright (c)        2017 The Shinoa Development Team.
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
    using Databases;
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using MySQL.Data.EntityFrameworkCore.Extensions;
    using Services;
    using Services.TimedServices;
    using YamlDotNet.Serialization;
    using static Logging;

    public static class Shinoa
    {
        public const string Version = "3.0.0-preview";

        public static readonly string VersionString =
            $"Shinoa v{Version}, built with love by OmegaVesko, FallenWarrior2k & Kazumi";

        private static readonly bool Alpha = Assembly.GetEntryAssembly().Location.ToLower().Contains("alpha");
        private static readonly CommandService Commands = new CommandService(new CommandServiceConfig
        {
            CaseSensitiveCommands = true,
            DefaultRunMode = RunMode.Async,
        });

        private static readonly IServiceCollection Map = new ServiceCollection();
        private static readonly Dictionary<Type, Func<Task>> Callbacks = new Dictionary<Type, Func<Task>>();
        private static IServiceProvider provider;
        private static Timer globalRefreshTimer;

        public static dynamic Config { get; private set; }

        public static CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        public static DiscordSocketClient Client { get; } = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Error,

            // Can't do LogSeverity.Warning because the continuous ratelimit warnings cause a StackOverflow
        });

        public static DateTime StartTime { get; } = DateTime.Now;

        public static void Main(string[] args) =>
            StartAsync().GetAwaiter().GetResult();

        public static async Task TryReenableLogging()
        {
            if (Client.ConnectionState != ConnectionState.Connected) return;
            string loggingChannelIdString = null;
            try
            {
                loggingChannelIdString = (string)Config["global"]["logging_channel_id"];
            }
            catch (KeyNotFoundException)
            {
                await LogError("The property was not found on the dynamic object. No logging channel was supplied.");
            }
            catch (Exception e)
            {
                await LogError(e.ToString());
            }

            if (loggingChannelIdString == null) return;
            if (ulong.TryParse(loggingChannelIdString, out ulong loggingChannelId))
            {
                if (Client.GetChannel(loggingChannelId) is IMessageChannel loggingChannel)
                {
                    await InitLoggingToChannel(loggingChannel);
                }
            }
        }

        public static async Task<bool> TryLeaveGuildAsync(ulong guildId)
        {
            try
            {
                var channels = Client.GetGuild(guildId).TextChannels
                    .Cast<IMessageChannel>().ToList();

                var services = typeof(Shinoa).GetTypeInfo()
                    .Assembly.GetExportedTypes()
                    .Select(t => t.GetTypeInfo())
                    .Where(t => t.GetInterfaces().Contains(typeof(IDatabaseService)) && !(t.IsAbstract || t.IsInterface));

                foreach (var service in services)
                {
                    var instance = provider.GetService(service.UnderlyingSystemType);
                    if (instance == null) continue;
                    if (instance is BlacklistService blacklistService)
                    {
                        await blacklistService.RemoveBinding(Client.GetGuild(guildId));
                        continue;
                    }

                    channels.ForEach(async channel =>
                    {
                        await ((IDatabaseService)instance).RemoveBinding(channel);
                        try
                        {
                            if (channel.Id.ToString() == (string)Config["global"]["logging_channel_id"]) StopLoggingToChannel();
                        }
                        catch (KeyNotFoundException)
                        {
                            await LogError("The property was not found on the dynamic object. No logging channel was supplied.");
                        }
                    });
                }

                await Client.GetGuild(guildId).LeaveAsync();
                return true;
            }
            catch (Exception e)
            {
                await LogError(e.ToString());
                return false;
            }
        }

        private static async Task StartAsync()
        {
            #region Prerequisites

            InitLoggingToFile();

            Console.Title = $"Shinoa v{Version}";

            var configurationFileStream = Alpha
                ? new FileStream("config_alpha.yaml", FileMode.Open)
                : new FileStream("config.yaml", FileMode.Open);

            using (configurationFileStream) // Ensure closure of FileStream object after the config file is loaded
            using (var streamReader = new StreamReader(configurationFileStream))
            {
                var deserializer = new Deserializer();
                Config = deserializer.Deserialize(streamReader);
                await Log("Config parsed and loaded.");
            }

            if (Alpha) await Log("Running in Alpha configuration.");

            var useUnicode = false;
            try
            {
                useUnicode = bool.TryParse(Config["global"]["unicode_logging"], out bool res) && res;
            }
            catch (KeyNotFoundException)
            {
                await Log("The property was not found on the dynamic object. Logging in UTF-8 by default.");
            }

            if (useUnicode) await Log("Logging in Unicode.");

            Console.OutputEncoding = useUnicode ? Encoding.Unicode : Encoding.UTF8;

            #endregion

            #region Modules

            Map.AddSingleton(Client);
            Map.AddSingleton(Commands);

            DatabaseProvider dbProvider;
            try
            {
                string providerString = Config["global"]["database"]["provider"];
                dbProvider = Enum.Parse<DatabaseProvider>(providerString);
            }
            catch (KeyNotFoundException)
            {
                await LogError("No database provider found. Exiting");
                return;
            }
            catch (ArgumentException)
            {
                await LogError("The given database provider was invalid. Exiting.");
                return;
            }

            try
            {
                string connectString = Config["global"]["database"]["connect_string"];
                var optionsBuilder = new DbContextOptionsBuilder();
                switch (dbProvider)
                {
                    case DatabaseProvider.PostgreSQL:
                        optionsBuilder.UseNpgsql(connectString);
                        break;
                    case DatabaseProvider.SQLServer:
                        optionsBuilder.UseSqlServer(connectString);
                        break;
                    case DatabaseProvider.MySQL:
                        optionsBuilder.UseMySQL(connectString);
                        break;
                    case DatabaseProvider.InMemory:
                        optionsBuilder.UseInMemoryDatabase($"{Guid.NewGuid()}");
                        break;
                    default:
                        await LogError("The given database provider was invalid. Exiting.");
                        break;
                }

                var contextOptions = optionsBuilder.Options;

                Map.AddSingleton(contextOptions);
            }
            catch (KeyNotFoundException)
            {
                await LogError("No database connection string was provided. Exiting.");
                return;
            }
            catch (Exception e)
            {
                await LogError("The given connection string was invalid. Exiting.");
                await LogError(e.ToString());
                return;
            }

            // Obsoleted because not concurrency-safe
            /*var databases = typeof(Shinoa).GetTypeInfo().Assembly.GetExportedTypes()
                    .Select(t => t.GetTypeInfo())
                    .Where(t => t.GetInterfaces().Contains(typeof(IDatabaseContext)) && !(t.IsAbstract || t.IsInterface))
                    .Select(t => t.UnderlyingSystemType);

            foreach (var database in databases)
            {
                Map.AddSingleton(database);
            }*/

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
                await Log($"Loaded module \"{module.Name}\"");
                foreach (var command in module.Commands) await Log($"Loaded command \"{command.Name}\"");
            }

            await Log($"Loaded {Commands.Modules.Count()} module(s) with {Commands.Commands.Count()} command(s)");

            #endregion

            #region Event handlers

            Client.Connected += async () =>
            {
                await Log($"Connected to Discord as {Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator}.");
                try
                {
                    await Client.SetGameAsync((string)Config["global"]["default_game"]);
                }
                catch (KeyNotFoundException)
                {
                    await Log("The property was not found on the dynamic object. No default game was supplied.");
                }
            };
            Client.Disconnected += async (e) =>
            {
                StopLoggingToChannel();
                await Log("Disconnected from Discord.");
                if (e != null)
                {
                    await LogError(e.ToString());
                }
            };
            Client.Log += async msg =>
            {
                await Log($"{msg.Severity}: {msg.Message}");
                if (msg.Exception != null) await LogError($"{msg.Source}: {msg.Exception.ToString()}");
            };
            Client.GuildAvailable += async g =>
            {
                await Log($"Connected to guild \"{g.Name}\".");
                string loggingChannelIdString = null;
                try
                {
                    loggingChannelIdString = (string)Config["global"]["logging_channel_id"];
                }
                catch (KeyNotFoundException)
                {
                    await LogError("The property was not found on the dynamic object. No logging channel was supplied.");
                }
                catch (Exception e)
                {
                    await LogError(e.ToString());
                }

                if (loggingChannelIdString == null) return;
                if (ulong.TryParse(loggingChannelIdString, out ulong loggingChannelId))
                {
                    if (Client.GetChannel(loggingChannelId) is IMessageChannel loggingChannel)
                    {
                        await InitLoggingToChannel(loggingChannel);
                    }
                }
            };
            Client.MessageReceived += async message =>
            {
                if (provider == null) return;
                var userMessage = message as SocketUserMessage;
                var argPos = 0;
                if (userMessage == null
                    || userMessage.Author.Id == Client.CurrentUser.Id
                    || !userMessage.HasStringPrefix((string)Config["global"]["command_prefix"], ref argPos)) return;

                var contextSock = new SocketCommandContext(Client, userMessage);
                await LogMessage(contextSock);
                var res = await Commands.ExecuteAsync(contextSock, argPos, provider);
                if (res.IsSuccess) return;

                await LogError(res.ErrorReason);
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
                    case CommandError.UnknownCommand:
                        break;
                    case CommandError.ObjectNotFound:
                        break;
                    case CommandError.MultipleMatches:
                        break;
                    case CommandError.Exception:
                        responseMessage = "An exception occurred.";
                        break;
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (responseMessage != string.Empty)
                {
                    var errorEmbed = new EmbedBuilder
                    {
                        Color = new Color(0xDD, 0, 0),
                        Title = res.ErrorReason,
                    };

                    try
                    {
                        await contextSock.Channel.SendMessageAsync(string.Empty, embed: errorEmbed);
                    }
                    catch (Exception e)
                    {
                        await LogError(e.ToString());
                    }
                }
            };
            Client.Ready += async () =>
            {
                foreach (var service in services)
                {
                    var instance = (IService)Activator.CreateInstance(service.UnderlyingSystemType);
                    var descriptor = new ServiceDescriptor(service.UnderlyingSystemType, instance);
                    if (Map.Contains(descriptor)) continue;

                    var configAttr = service.GetCustomAttribute<ConfigAttribute>();
                    dynamic config = null;
                    try
                    {
                        config = configAttr?.ConfigName != null ? Config[configAttr.ConfigName] : null;
                    }
                    catch (KeyNotFoundException)
                    {
                        await LogError($"The property was not found on the dynamic object. No service settings for \"{service.Name}\" were supplied.");
                    }
                    catch (Exception e)
                    {
                        await LogError(e.ToString());
                    }

                    try
                    {
                        provider = Map.BuildServiceProvider();
                        instance.Init(config, provider);
                        Map.AddSingleton(service.UnderlyingSystemType, instance);
                    }
                    catch (Exception e)
                    {
                        await LogError($"Initialization of service \"{service.Name}\" failed.");
                        await LogError(e.ToString());
                        Map.Remove(descriptor);
                    }

                    if (instance is ITimedService timedService)
                    {
                        Callbacks.TryAdd(service.UnderlyingSystemType, timedService.Callback);
                        await Log($"Service \"{service.Name}\" added to callbacks");
                    }

                    await Log($"Loaded service \"{service.Name}\"");
                }

                var refreshRate = 30;
                try
                {
                    refreshRate = int.Parse(Config["global"]["refresh_rate"]);
                }
                catch (KeyNotFoundException)
                {
                    await LogError(
                        "The property was not found on the dynamic object. No global refresh rate was supplied. Defaulting to once every 30 seconds.");
                }
                catch (Exception e)
                {
                    await LogError(e.ToString());
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
                                await LogError(e.ToString());
                            }
                        }
                    },
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(refreshRate));

                provider = Map.BuildServiceProvider();
                await Log("All modules initialized successfully. Shinoa is up and running.");
            };

            #endregion

            #region Connection establishment

            await Log("Connecting to Discord...");
            await Client.LoginAsync(TokenType.Bot, (string)Config["global"]["token"]);
            await Client.StartAsync();

            var completionSource = new TaskCompletionSource<object>();
            Cts.Token.Register(() => completionSource.TrySetCanceled());
            var blockTask = Task.Delay(-1, Cts.Token);
            await Task.WhenAny(blockTask, completionSource.Task);
            await Client.LogoutAsync();
            await Client.StopAsync();
            await Log("Exiting.");

            #endregion
        }
    }
}
