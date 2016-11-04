using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Log;
using Unosquare.Labs.EmbedIO.Modules;

namespace Shinoa.Net
{
    class ShinoaNet
    {
        public static DateTime StartTime = DateTime.Now;

        public static string AppName = "Shinoa.Net";
        public static string VersionId = "0.0";

        public static dynamic Config;
        public static DiscordClient DiscordClient;

        public static Module.IModule[] ActiveModules =
        {
            new Module.AdminModule(),
            new Module.ChatterModule(),
            new Module.AnimeNotificationsModule(),
            new Module.AnilistModule(),
            new Module.DocsModule(),
            new Module.SAOWikiModule(),
            new Module.WikipediaModule(),
            new Module.MALSearchModule(),
            new Module.MALMangaSearchModule(),
            new Module.RedditModule(),
            new Module.AnidbGraphModule(),
            new Module.TwitterModule(),
            new Module.JishoModule(),
            new Module.BackstrokeModule(),
            new Module.ModerationModule(),
            new Module.CleverbotModule(),
            new Module.WelcomeModule()
        };

        static void Main(string[] args)
        {
            using (var streamReader = new StreamReader("config.yaml"))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                ShinoaNet.Config = deserializer.Deserialize(streamReader);

                ShinoaNet.VersionId = Config["version_id"];
                Logging.Log("Successfully loaded configuration.");
            }

            Console.Title = $"{ShinoaNet.AppName} ver. {ShinoaNet.VersionId}";
            Logging.Log($"{ShinoaNet.AppName} ver. {ShinoaNet.VersionId}");

            ShinoaNet.DiscordClient = new DiscordClient(x =>
            {
                x.AppName = ShinoaNet.AppName;
            });

            using (var server = new WebServer(Config["web_server_url"], new SimpleConsoleLog()))
            {
                server.RegisterModule(new StaticFilesModule("WebPublic"));
                server.Module<StaticFilesModule>().DefaultExtension = ".html";

                server.RegisterModule(new WebApiModule());
                server.Module<WebApiModule>().RegisterController<RestController>();

                server.RunAsync();

                ShinoaNet.DiscordClient.ExecuteAndWait(async () =>
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

                    Logging.Log($"Connected to Discord as @{DiscordClient.CurrentUser.Name}.");

                    Logging.Log("=====================");

                    DiscordClient.SetGame(Config["default_game"]);

                    DiscordClient.MessageReceived += (s, e) =>
                    {
                        if (e.Message.Channel.IsPrivate)
                        {
                            if (e.Message.User.Id != DiscordClient.CurrentUser.Id)
                            {
                                Logging.Log($"[PM] {e.User.Name}: {e.Message.Text}");
                            }
                        }
                    };

                    foreach (var module in ActiveModules)
                    {
                        Logging.Log($"Binding module {module.GetType().Name}.");

                        module.Init();
                        DiscordClient.MessageReceived += module.MessageReceived;
                    }

                    Logging.InitLoggingToChannel();
                });

                while (true) Console.ReadKey();
            }
        }

        public static void WriteConfig()
        {
            using (var streamWriter = new StreamWriter("config.yaml", false))
            {
                var serializer = new YamlDotNet.Serialization.Serializer();
                serializer.Serialize(streamWriter, Config);
            }
        }
    }
}
