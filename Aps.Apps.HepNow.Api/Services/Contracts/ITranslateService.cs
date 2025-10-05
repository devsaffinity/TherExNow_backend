namespace Aps.Apps.HepNow.Services.Contracts;

public interface ITranslateService
{
    ValueTask<ResponseBase<string>> AmazonTranslateAsync(TranslateInput input);
}
