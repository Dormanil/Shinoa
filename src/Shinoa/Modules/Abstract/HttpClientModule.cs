using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using System.Net.Http;
using System.Text;

namespace Shinoa.Modules.Abstract
{
    public abstract class HttpClientModule : Module
    {
        protected HttpClient HttpClient = new HttpClient();

        protected string BaseUrl
        {
            set
            {
                HttpClient.BaseAddress = new Uri(value);
            }

            get
            {
                return HttpClient.BaseAddress.AbsoluteUri;
            }
        }

        protected void SetBasicHttpCredentials(string username, string password)
        {
            var byteArray = Encoding.ASCII.GetBytes($"{username}:{password}");
            HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        protected string HttpGet(string relativeUrl)
        {
            var response = HttpClient.GetAsync(relativeUrl).Result;
            var content = response.Content;
            return content.ReadAsStringAsync().Result;
        }

        protected string HttpPost(string relativeUrl, HttpContent httpContent)
        {
            var response = HttpClient.PostAsync(relativeUrl, httpContent).Result;
            var content = response.Content;
            return content.ReadAsStringAsync().Result;
        }
    }
}
