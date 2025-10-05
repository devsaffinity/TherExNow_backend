using Amazon;
using Amazon.Runtime;
using Amazon.Translate;
using Amazon.Translate.Model;
using Aps.Apps.HepNow.Models.Dtos;
using Microsoft.Extensions.Options;
using System.Text;

namespace Aps.Apps.HepNow.GraphQL.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class TranslatorMutations
    {
        [GraphQLName("translator_translate")]
        public async Task<ResponseBase<string>> Translate([Authentication] Authentication authentication, [Service] ITranslateService service, TranslateInput input)
        {
           #region obsolete
            /*string route = $"/translate?api-version=3.0&from={input.FromLanguage}&to={input.ToLanguage}";
            object[] body = new object[] { new { Text = input.Text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(options.Value.Endpoint + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", options.Value.Key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", options.Value.Location);

                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject(json as dynamic)[0].translations[0].text.Value;
                }
                else
                    return ResponseStatus.Failed;
            }*/

            //BasicAWSCredentials credentials = new(options.Value.AccessKey, options.Value.SecretKey);
            //var client = new AmazonTranslateClient(credentials, RegionEndpoint.GetBySystemName(options.Value.Region));

            //var request = new TranslateTextRequest
            //{
            //    SourceLanguageCode = input.FromLanguage,
            //    TargetLanguageCode = input.ToLanguage,
            //    Text = input.Text,
            //};

            //var response = await client.TranslateTextAsync(request);
            //return response.TranslatedText;



            //public class TranslatorConfig
            //{
            //    public string AccessKey { get; set; }
            //    public string SecretKey { get; set; }
            //    public string Region { get; set; }
            //}
            #endregion

           return await service.AmazonTranslateAsync(input);

        }
    }
}

