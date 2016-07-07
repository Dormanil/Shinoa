using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using RestSharp;
using System.Text.RegularExpressions;
using System.IO;

namespace Shinoa.Net.Module
{
    class AnidbGraphModule : IModule
    {
        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            return;
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"animerel (?<querytext>.*)");
            if (regex.IsMatch(e.Message.Text))
            {
                var queryText = regex.Matches(e.Message.Text)[0].Groups["querytext"];

                StreamReader file = new StreamReader(@"Resources\anime-titles.dat.gz");
                string line;

                var separator = new char[] { '|' };

                while ((line = file.ReadLine()) != null)
                {
                    if (line[0] != '#')
                    {
                        var components = line.Split(separator);
                        var id = components[0];
                        var title = components[3];

                        if (queryText.Value.ToLower().Trim() == title.ToLower().Trim())
                        {
                            e.Channel.SendMessage($"http://anidb.net/pics/graph/a-{id}.png");
                            file.Close();
                            return;
                        }
                    }
                }

                e.Channel.SendMessage($"Title not found in database.");
                file.Close();
            }
        }
    }
}
