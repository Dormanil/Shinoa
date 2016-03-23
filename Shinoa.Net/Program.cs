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
        public static string AppName = "Shinoa.Net";
        public static string VersionId = "0.0";

        public static dynamic Config;
        public static DiscordClient DiscordClient;

        static Module.IModule[] ActiveModules = 
        {
            new Module.StaticModule()
        };

        static void Main(string[] args)
        {
            using (var streamReader = new StreamReader("config.yaml"))
            {
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                ShinoaNet.Config = deserializer.Deserialize(streamReader);

                ShinoaNet.VersionId = Config["version_id"];
                Console.WriteLine("Successfully loaded configuration.");
            }

            Console.Title = $"{ShinoaNet.AppName} ver. {ShinoaNet.VersionId}";
            Console.WriteLine($"{ShinoaNet.AppName} ver. {ShinoaNet.VersionId}");

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

                Console.WriteLine($"Connected to Discord as @{DiscordClient.CurrentUser.Name}.");
                Console.WriteLine("=====================");

                DiscordClient.MessageReceived += (s, e) =>
                {
                    if (e.Message.RawText.Contains($"<@{DiscordClient.CurrentUser.Id}>"))
                    {
                        // Someone mentioned the bot.
                        Console.WriteLine($"[{e.Server.Name} -> #{e.Channel.Name}] {e.Message.User.Name}: {e.Message.Text}");
                    }                    
                };

                foreach(var module in ActiveModules)
                {
                    DiscordClient.MessageReceived += module.MessageReceived;
                }
            });

            while (true) Console.ReadKey();
        }
    }
}
