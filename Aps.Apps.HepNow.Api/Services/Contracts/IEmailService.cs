namespace Aps.Apps.HepNow.Services.Contracts;

public interface IEmailService
{
    ValueTask<ResponseBase> SendEmailByAmazonSesAsync(EmailTemplateDto emailTemplate);
}
