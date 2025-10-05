using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

namespace Aps.Apps.HepNow.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async ValueTask<ResponseBase> SendEmailByAmazonSesAsync(EmailTemplateDto emailTemplate)
    {
        try
        {
            var sesCredentials = _configuration.GetSection("SES-Credentials").Get<SesCredentials>()!;

            BasicAWSCredentials credentials = new(sesCredentials.AccessKey, sesCredentials.SecretKey);
            IAmazonSimpleEmailService emailService = new AmazonSimpleEmailServiceClient(credentials, RegionEndpoint.GetBySystemName(sesCredentials.Region));

            var response = await emailService.SendEmailAsync(new SendEmailRequest
            {
                Destination = new()
                {
                    ToAddresses = new List<string> { emailTemplate.EmailRecipient }
                },
                Message = new Message
                {
                    Body = new Body
                    {
                        Html = new Content(emailTemplate.Body)
                    },
                    Subject = new Content(emailTemplate.Subject)
                },
                Source = sesCredentials.SendEmailAfterRegister
            });

            return response.HttpStatusCode == System.Net.HttpStatusCode.OK ? ResponseStatus.Success : ResponseStatus.Failed;
        }
        catch
        {
            return ResponseStatus.Failed;
        }
    }
}

public class SesCredentials
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string Region { get; set; }
    public string EmailFrom { get; set; }
    public string SendEmailAfterRegister { get; set; }
}


public class EmailTemplateDto
{
    public string EmailRecipient { get; set; }

    public string Subject { get; set; }
    public string Body { get; set; }
}
