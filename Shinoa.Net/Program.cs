using Discord;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            new Module.StaticModule(),
            new Module.AdminModule(),
            new Module.ChatterModule(),
            new Module.AnimeNotificationsModule(),
            new Module.AnilistModule()
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

            ShinoaNet.DiscordClient.ExecuteAndWait(async () =>
            {
                while(true)
                {
                    try
                    {
                        await DiscordClient.Connect(Config["email"], Config["password"]);
                        DiscordClient.SetGame(Config["default_game"]);
                        break;
                    }
                    catch (Exception ex)
                    {
                        DiscordClient.Log.Error($"Login Failed", ex);
                        await Task.Delay(DiscordClient.Config.FailedReconnectDelay);
                    }
                }

                Logging.Log($"Connected to Discord as @{DiscordClient.CurrentUser.Name}.");
                Logging.Log("=====================");

                DiscordClient.MessageReceived += (s, e) =>
                {
                    if (e.Message.RawText.Contains($"<@{DiscordClient.CurrentUser.Id}>"))
                    {
                        // Someone mentioned the bot.
                        //Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] {e.Message.User.Name}: {e.Message.Text}");
                        //Logging.LogMessage(e.Message);
                    }       
                    else if (e.Message.Channel.IsPrivate)
                    {
                        Logging.Log($"[PM] {e.User.Name}: {e.Message.Text}");
                    }
                };

                foreach(var module in ActiveModules)
                {
                    Logging.Log($"Binding module {module.GetType().Name}.");

                    module.Init();
                    DiscordClient.MessageReceived += module.MessageReceived;
                }
            });

            while (true) Console.ReadKey();
        }
    }
}
