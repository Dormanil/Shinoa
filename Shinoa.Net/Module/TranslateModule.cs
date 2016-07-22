using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using RestSharp;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Shinoa.Net.Module
{
    class TranslateModule : IModule
    {
        static RestClient RestClient = new RestClient("https://translate.googleapis.com");

        public string DetailedStats()
        {
            return null;
        }

        public void Init()
        {
            
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"translate (?<querytext>.*)");
                if (regex.IsMatch(e.Message.Text))
                {
                    var queryText = regex.Matches(e.Message.Text)[0].Groups["querytext"].Value;

                    Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] {e.User.Name} translated '{queryText}'.");

                    var request = new RestRequest($"translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={System.Uri.EscapeUriString(queryText)}");

                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);
                    }
                    
                    //try
                    //{
                    //    dynamic responseObject = JsonConvert.DeserializeObject(response.Content, );
                    //    var translation = responseObject[0][0][0];

                    //    byte[] data = Encoding.Default.GetBytes(translation);
                    //    translation = Encoding.UTF8.GetString(data);

                    //    e.Channel.SendMessage($"Translation: {translation}");
                    //}
                    //catch (Exception ex)
                    //{
                    //    e.Channel.SendMessage("Translation not successful.");
                    //    Logging.Log(ex.ToString());
                    //}
                }
            }
        }
    }
}
