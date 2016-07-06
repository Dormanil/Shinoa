using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using RestSharp;
using Newtonsoft.Json;
using System.Timers;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Microsoft.CSharp.RuntimeBinder;

namespace Shinoa.Net.Module
{
    class AnilistModule : IModule
    {
        static RestClient RestClient = new RestClient("https://anilist.co/api/");

        static string ClientId;
        static string ClientSecret;
        static string AccessToken;

        // Refresh the token every 30 minutes (expires at 60).
        static Timer TokenRefreshTimer = new Timer { Interval = 1000 * 60 * 30 };

        public void Init()
        {
            ClientId = ShinoaNet.Config["anilist_client_id"];
            ClientSecret = ShinoaNet.Config["anilist_client_secret"];

            RefreshClientAccessToken();

            TokenRefreshTimer.Elapsed += (s, e) =>
            {
                RefreshClientAccessToken();
            };

            TokenRefreshTimer.Start();
        }

        public void MessageReceived(object sender, MessageEventArgs e)
        {
            if (e.User.Id != ShinoaNet.DiscordClient.CurrentUser.Id)
            {
                // var regex = new Regex(@"{{(?<animetitle>.*)}}");
                var regex = new Regex(@"^" + ShinoaNet.Config["command_prefix"] + @"anilist (?<animetitle>.*)");
                if (regex.IsMatch(e.Message.Text))
                {
                    var animeTitle = regex.Matches(e.Message.Text)[0].Groups["animetitle"];

                    var request = new RestRequest($"anime/search/{animeTitle}");
                    request.AddParameter("access_token", AccessToken);

                    // Retry until the request successfully goes through.

                    IRestResponse response = null;
                    while (response == null)
                    {
                        response = RestClient.Execute(request);                        
                    }

                    dynamic responseObject = JsonConvert.DeserializeObject(response.Content);

                    Logging.Log($"[{e.Server.Name} -> #{e.Channel.Name}] @{e.User.Name} requested anime '{animeTitle}'.");

                    try
                    {
                        dynamic firstResult = responseObject[0];

                        var responseMessage = "";
                        responseMessage += $"Japanese title: **{firstResult["title_japanese"]}**\n";
                        responseMessage += $"Romanized title: **{firstResult["title_romaji"]}**\n";
                        responseMessage += $"English title: **{firstResult["title_english"]}**\n";

                        if (firstResult["synonyms"].Count > 0)
                        {
                            responseMessage += "Synonyms: ";
                            foreach (var synonym in firstResult["synonyms"])
                            {
                                responseMessage += $"{synonym}, ";
                            }
                            responseMessage = responseMessage.Trim(new char[] { ' ', ',' });
                            responseMessage += "\n";
                        }

                        responseMessage += $"Airing status: {firstResult["airing_status"]}\n";
                        responseMessage += $"Average score (0-100): {firstResult["average_score"]}\n";
                        responseMessage += $"Episode count: {firstResult["total_episodes"]}\n";

                        responseMessage += $"\nhttp://anilist.co/anime/{firstResult["id"]}";

                        e.Channel.SendMessage(responseMessage);

                        WebClient webclient = new WebClient();
                        webclient.DownloadFile($"{firstResult["image_url_lge"]}", $"{Path.GetTempPath()}anime_cover_{firstResult["id"]}.jpg");

                        e.Channel.SendFile($"{Path.GetTempPath()}anime_cover_{firstResult["id"]}.jpg");
                    }
                    catch (ArgumentException ex)
                    {
                        Logging.Log("Anilist request encountered an ArgumentException.");
                        Logging.Log($"ResponseObject is: {responseObject}");

                        RefreshClientAccessToken();

                    }
                    catch(RuntimeBinderException ex)
                    {
                        e.Channel.SendMessage("Anime not found.");
                    }
                    catch (Exception ex)
                    {
                        e.Channel.SendMessage("Error encountered, anime not found.");
                        Logging.Log(ex.ToString());
                    }
                }
            }
        }

        void RefreshClientAccessToken()
        {
            // Logging.Log("Refreshing Anilist access token...");

            var request = new RestRequest("auth/access_token", Method.POST);
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("client_id", ClientId);
            request.AddParameter("client_secret", ClientSecret);


            IRestResponse response = null;
            while (response == null)
            {
                response = RestClient.Execute(request);                
            }

            dynamic responseObject = JsonConvert.DeserializeObject(response.Content);
            AccessToken = responseObject.access_token;

            // Logging.Log($"Refreshed Aniist access token: {AccessToken}");
        }

        public string DetailedStats()
        {
            return null;
        }
    }
}
