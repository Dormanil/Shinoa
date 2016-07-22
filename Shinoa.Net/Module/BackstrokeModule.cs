using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;
using SubtitlesParser.Classes;
using System.Text.RegularExpressions;

namespace Shinoa.Net.Module
{
    class BackstrokeModule : IModule
    {
        private static List<SubtitleItem> SubtitleCollection;

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            var parser = new SubtitlesParser.Classes.Parsers.SubParser();
            using (var fileStream = File.OpenRead(@"Resources\backstroke.srt"))
            {
                SubtitleCollection = parser.ParseStream(fileStream);
            }
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"backstroke");
            if (regex.IsMatch(e.Message.Text))
            {
                var index = new Random().Next(SubtitleCollection.Count);
                var lines = SubtitleCollection[index].Lines;

                var response = "";
                foreach (var line in lines) response += line + "\n";

                e.Channel.SendMessage(response.Trim());
            }
        }
    }
}
