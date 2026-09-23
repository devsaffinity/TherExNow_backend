using Amazon.Runtime;
using Amazon.Translate.Model;
using Amazon.Translate;
using Microsoft.Extensions.Options;
using Amazon;
using Amazon.Runtime.Internal;

namespace Aps.Apps.HepNow.Services;

public class TranslateService : ITranslateService
{
    private readonly IOptions<TranslatorConfig> options;

    public TranslateService(IOptions<TranslatorConfig> options)
    {
        this.options = options;
    }

    public async ValueTask<ResponseBase<string>> AmazonTranslateAsync(TranslateInput input)
    {
        int maxLength = 5000; // Limit AWS Translate

        // Nothing to translate. Amazon rejects an empty request outright ("Text size cannot be
        // zero"), which surfaced as a 500 and took whole exercise programs down with it: a client
        // translating a program string by string hits this on the first exercise with a blank note
        // or a blank instruction step, and the shared program then arrived with no instructions at
        // all. A blank string translates to itself, so answer it here rather than calling out.
        if (string.IsNullOrWhiteSpace(input?.Text))
            return new ResponseBase<string>(input?.Text ?? string.Empty);

        if (input.Text.Length > maxLength)
            return CustomResponseStatus.LimitOfMoreThan5000Characters;

        BasicAWSCredentials credentials = new(options.Value.AccessKey, options.Value.SecretKey);
        var client = new AmazonTranslateClient(credentials, RegionEndpoint.GetBySystemName(options.Value.Region));

        var request = new TranslateTextRequest
        {
            SourceLanguageCode = input.FromLanguage,
            TargetLanguageCode = input.ToLanguage,
            Text = input.Text
        };

   
        var response = await client.TranslateTextAsync(request);
        return new ResponseBase<string>(response.TranslatedText);
    }

    private static List<string> SplitTextSmartly(string text, int maxCharacters)
    {
        var result = new List<string>();
        int currentIndex = 0;

        while (currentIndex < text.Length)
        {
            int length = Math.Min(maxCharacters, text.Length - currentIndex);
            int splitIndex = currentIndex + length;

            //Search for a suitable point or interval in the range of 4500 to 5000
            if (splitIndex < text.Length)
            {
                int lastPeriod = text.LastIndexOf('.', splitIndex, 500);
                int lastNewLine = text.LastIndexOf('\n', splitIndex, 500);

                //Find the last period or paragraph before 5000 characters
                int bestSplit = Math.Max(lastPeriod, lastNewLine);
                if (bestSplit > currentIndex)
                {
                    splitIndex = bestSplit + 1;
                }
            }

            result.Add(text.Substring(currentIndex, splitIndex - currentIndex).Trim());
            currentIndex = splitIndex;
        }

        return result;
    }
}

public class TranslatorConfig
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Region { get; set; }
}
