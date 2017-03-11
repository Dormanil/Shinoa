using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord.Commands;
using Newtonsoft.Json;

namespace Shinoa.Modules
{
    public class JapaneseDictModule : ModuleBase<SocketCommandContext>
    {
        static readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://jisho.org/api/v1/search/") };

        [Command("jp"), Alias("jisho", "jpdict", "japanese")]
        public async Task JishoSearch([Remainder] string term)
        {
            var responseMessageTask = ReplyAsync("Searching...");

            var httpResponseText = httpClient.HttpGet($"words?keyword={term}");
            dynamic responseObject = JsonConvert.DeserializeObject(httpResponseText);

            var responseMessage = await responseMessageTask;

            try
            {
                dynamic firstResult = responseObject["data"][0];

                var responseText = "";

                foreach (var word in firstResult["japanese"])
                {
                    var wordKanji = word["word"];
                    var wordReading = word["reading"];

                    if (wordKanji != null && wordReading != null) responseText += $"**{wordKanji}** - {wordReading}, ";
                    else if (wordKanji != null) responseText += $"**{wordKanji}**, ";
                    else if (wordReading != null) responseText += $"**{wordReading}**, ";
                }

                responseText = responseText.Trim(',', ' ');
                responseText += '\n';

                foreach (var sense in firstResult["senses"])
                {
                    responseText += "\u2022 ";

                    foreach (var definition in sense["english_definitions"])
                    {
                        responseText += $"{definition}, ";
                    }

                    responseText = responseText.Trim(',', ' ');

                    if (sense["parts_of_speech"].Count > 0)
                    {
                        responseText += " (";

                        foreach (string part in sense["parts_of_speech"])
                        {
                            responseText += $"{part.ToLower()}, ";
                        }

                        responseText = responseText.Trim(',', ' ');

                        responseText += ")";
                    }

                    responseText += '\n';
                }

                responseText += $"\nSee more: <http://jisho.org/search/{Uri.EscapeUriString(term)}>";

                await responseMessage.ModifyAsync(p => p.Content = responseText);
            }
            catch (Exception)
            {
                await responseMessage.ModifyAsync(p => p.Content = "Not found.");
            }
        }
    }
}
