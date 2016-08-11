using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Modules;

namespace Shinoa.Net
{
    class RestController : WebApiController
    {
        class Server
        {
            public class Channel
            {
                public ulong id;
                public string name;
            }

            public ulong id;
            public string name;
            public List<Channel> channels = new List<Channel>();
        }

        [WebApiHandler(HttpVerbs.Post, "/api/send")]
        public bool SendMessage(WebServer server, HttpListenerContext context)
        {
            try
            {
                var requestBody = new StreamReader(context.Request.InputStream).ReadToEnd();
                dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(requestBody);

                string channelId = data["channel_id"];
                string message = data["message"];

                var channel = ShinoaNet.DiscordClient.GetChannel(ulong.Parse(channelId));
                channel.SendMessage(message);

                return true;
            }
            catch (Exception ex)
            {
                return HandleError(context, ex, (int)HttpStatusCode.InternalServerError);
            }
        }

        [WebApiHandler(HttpVerbs.Get, "/api/servers")]
        public bool GetServers(WebServer server, HttpListenerContext context)
        {
            try
            {
                List<Server> servers = new List<Server>();

                foreach (var discordServer in ShinoaNet.DiscordClient.Servers)
                {
                    var newServer = new Server();
                    newServer.id = discordServer.Id;
                    newServer.name = discordServer.Name;
                    
                    foreach(var channel in discordServer.TextChannels)
                    {
                        var newChannel = new Server.Channel();
                        newChannel.name = channel.Name;
                        newChannel.id = channel.Id;

                        newServer.channels.Add(newChannel);
                    }

                    servers.Add(newServer);
                }

                return context.JsonResponse(servers);
            }
            catch (Exception ex)
            {
                return HandleError(context, ex, (int)HttpStatusCode.InternalServerError);
            }
        }

        protected bool HandleError(HttpListenerContext context, Exception ex, int statusCode = 500)
        {
            var errorResponse = new
            {
                Title = "Unexpected Error",
                ErrorCode = ex.GetType().Name,
                Description = ex.ExceptionMessage(),
            };

            context.Response.StatusCode = statusCode;
            return context.JsonResponse(errorResponse);
        }


    }

}
