using Aps.Apps.HepNow.Generics.Responses;
using Aps.Apps.HepNow.Services;
using Microsoft.Extensions.Options;

namespace Aps.Apps.HepNow.Test.ServiceTest
{
    /// <summary>
    /// Nothing here reaches Amazon: the credentials are deliberately invalid, so any test that did
    /// call out would fail. That is the point -- a blank string must be answered locally.
    /// </summary>
    public class TranslateTest
    {
        private static TranslateService Service() =>
            new(Options.Create(new TranslatorConfig
            {
                AccessKey = "not-a-real-key",
                SecretKey = "not-a-real-secret",
                Region = "us-west-1"
            }));

        /// <summary>
        /// Amazon answers an empty request with "Text size cannot be zero", which used to escape as
        /// a 500. A client walking a program string by string hits it on the first blank note or
        /// blank instruction step, and the whole program then shared with no instructions at all.
        /// </summary>
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\n")]
        [InlineData("\t  \r\n ")]
        public async Task BlankTextIsReturnedWithoutCallingAmazon(string text)
        {
            var response = await Service().AmazonTranslateAsync(new TranslateInput
            {
                Text = text,
                FromLanguage = "en",
                ToLanguage = "es"
            });

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(text ?? string.Empty, response.Result);
        }

        [Fact]
        public async Task NullInputIsRefusedRatherThanThrowing()
        {
            var response = await Service().AmazonTranslateAsync(null);

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(string.Empty, response.Result);
        }

        /// <summary>
        /// The existing length guard has to keep running after the blank check, and has to do so
        /// without calling out either.
        /// </summary>
        [Fact]
        public async Task TextOverTheAmazonLimitIsStillRefused()
        {
            var response = await Service().AmazonTranslateAsync(new TranslateInput
            {
                Text = new string('a', 5001),
                FromLanguage = "en",
                ToLanguage = "es"
            });

            Assert.Equal(CustomResponseStatus.LimitOfMoreThan5000Characters, response.Status);
        }
    }
}
