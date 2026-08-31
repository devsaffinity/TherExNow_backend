using Aps.Apps.HepNow.Extensions;
using Aps.Apps.HepNow.Models.Dtos;
using Aps.Apps.HepNow.Models.Inputs;
using Aps.Apps.HepNow.Services.Contracts;
using FirebaseAdmin.Auth;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Aps.Apps.HepNow.Services;

public class UserService : UserServiceBase<Users, UserInput>, IUserService
{
    public UserService(IUserRepository repository, IOptions<SendGridConfig> sendGridConfig, IOptions<TwilioConfig> twilioConfig, FirebaseAppCreator firebaseAppCreator, IEmailService emailService, ILogger<UserService> logger, IHttpContextAccessor httpContextAccessor) : base(repository)
    {
        this.repository = repository;
        this.sendGridConfig = sendGridConfig;
        this.twilioConfig = twilioConfig;
        this.firebaseAppCreator = firebaseAppCreator;
        _emailService = emailService;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    #region props
    private IUserRepository repository;
    private readonly IOptions<SendGridConfig> sendGridConfig;
    private readonly IOptions<TwilioConfig> twilioConfig;
    private readonly FirebaseAppCreator firebaseAppCreator;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    #endregion

    #region functions


    public override ResponseBase<Users> SignUp(string externalId, string? email, UserInput userInput)
    {
        var user = repository.Where(a => a.ExternalId == externalId || a.ExternalId2 == externalId || (!string.IsNullOrEmpty(userInput.Email) && a.Email == userInput.Email)).IgnoreQueryFilters().FirstOrDefault();
        if (user == null)
            return base.SignUp(externalId, email, userInput);
        
        var callerOwnsRow = user.ExternalId == externalId || user.ExternalId2 == externalId;

        // The lookup above can also match on userInput.Email, which is client-supplied, so a caller
        // can name any account. Claiming a row that is not already keyed to this Firebase UID
        // therefore needs a provider-verified address on the token, matched against the row's own
        // email — not against userInput.Email. Firebase issues a token the moment an email/password
        // account is created, so without this, registering a victim's address and calling
        // user_signUp attaches the caller's UID to the victim's account. Returning early also keeps
        // an unverified caller from restoring, mutating, or reading back a row it does not own.
        if (!callerOwnsRow)
        {
            var claims = _httpContextAccessor.HttpContext?.User;
            if (!FirebaseClaims.IsEmailVerified(claims) || string.IsNullOrEmpty(email) || user.Email != email
                || user.ExternalId2 != null)
                return ResponseStatus.AlreadyExists;
        }

        if (user.IsDeleted)
            user.IsDeleted = false;
        else if (callerOwnsRow)
            return ResponseStatus.AlreadyExists;

        if (!callerOwnsRow)
            user.ExternalId2 = externalId;

        repository.Update(user);
        return ResponseBase<Users>.Success(user);
    }

    public override ResponseBase<Users> Remove(int entityId)
    {
        var user = repository.Find(entityId);
        if (user == null)
            return ResponseStatus.UserNotFound;

        repository.Remove(user);
        try
        {
            var auth = FirebaseAuth.GetAuth(firebaseAppCreator.GetFirebaseApp());
            auth.DeleteUserAsync(user.ExternalId).GetAwaiter().GetResult();

            if (!string.IsNullOrEmpty(user.ExternalId2))
                auth.DeleteUserAsync(user.ExternalId2).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Firebase user for local user {UserId}", entityId);
        }

        return ResponseStatus.Success;
    }

    //obsolete
    public ResponseBase<ResponseStatus> SendEmail(EmailInput emailInput, int? userId)
    {
        try
        {
            //if (!emailInput.Content.Contains("</"))
            emailInput.Content = "<p>" + emailInput.Content + "</p>";

            var client = new SendGridClient(sendGridConfig.Value.ApiKey);
            var from = new EmailAddress(sendGridConfig.Value.EmailFrom, "");
            var to = new EmailAddress(emailInput.To, "");
            var msg = MailHelper.CreateSingleEmail(from, to, emailInput.Subject, null, emailInput.Content);
            var response = client.SendEmailAsync(msg).ConfigureAwait(false).GetAwaiter().GetResult();

            if (response.IsSuccessStatusCode)
            {
                if (userId > 0)
                {
                    var user = repository.GetById(userId);
                    if (user != null)
                    {
                        user.SentEmailCount++;
                        repository.Update(user);
                    }
                }

                return ResponseBase<ResponseStatus>.Success(ResponseStatus.Success);
            }
            else
            {
                return ResponseBase<ResponseStatus>.Failure(ResponseStatus.UnknownError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} for user {UserId}", emailInput.To, userId);
            return ResponseBase<ResponseStatus>.Failure(ResponseStatus.UnknownError);
        }
    }

    public async ValueTask<ResponseBase<ResponseStatus>> SendEmailByAmazonSesAsync(EmailInput emailInput, int? userId)
    {
        try
        {
            //if (!emailInput.Content.Contains("</"))
            emailInput.Content = "<p>" + emailInput.Content + "</p>";


           var response = await _emailService.SendEmailByAmazonSesAsync(new EmailTemplateDto
            {
                Body = emailInput.Content,
                Subject = emailInput.Subject,
                EmailRecipient = emailInput.To
            });

            
            if (response.Status == ResponseStatus.Success)
            {
                if (userId > 0)
                {
                    var user = await repository.GetByIdAsync(userId);
                    if (user != null)
                    {
                        user.SentEmailCount++;
                        await repository.UpdateAsync(user);
                    }
                }

                return ResponseBase<ResponseStatus>.Success(ResponseStatus.Success);
            }
            else
            {
                return ResponseBase<ResponseStatus>.Failure(ResponseStatus.UnknownError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via Amazon SES to {To} for user {UserId}", emailInput.To, userId);
            return ResponseBase<ResponseStatus>.Failure(ResponseStatus.UnknownError);
        }
    }

    public ResponseBase<ResponseStatus> SendSms(SmsInput smsInput, int userId)
    {
        try
        {
            //Call web service to send SMS
            var accountSID = twilioConfig.Value.AccountSID;
            var authToken = twilioConfig.Value.AuthToken;
            var fromNumber = twilioConfig.Value.FromNumber;

            TwilioClient.Init(accountSID, authToken);

            var message = MessageResource.Create(
                body: smsInput.Text,
                from: new Twilio.Types.PhoneNumber(fromNumber),
                to: new Twilio.Types.PhoneNumber(smsInput.PhoneNumber)
            );

            if (!string.IsNullOrEmpty(message.Sid))
            {
                var user = repository.GetById(userId);
                if (user != null)
                {
                    user.SentSmsCount++;
                    repository.Update(user);
                }
                return ResponseBase<ResponseStatus>.Success(ResponseStatus.Success);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to send SMS to {PhoneNumber} for user {UserId}", smsInput.PhoneNumber, userId);
        }

        return ResponseBase<ResponseStatus>.Failure(ResponseStatus.UnknownError);
    }

    public ListResponseBase<UserDto> GetUsers()
    {
        return ListResponseBase<UserDto>.Success(repository.GetUsers());
    }

    public ResponseBase<Users> ConfirmUser(string confirmKey)
    {
        var user = repository.Where(r => r.ConfirmKey == confirmKey).FirstOrDefault();
        if (user == null)
            return ResponseStatus.UserNotFound;

        user.IsConfirmed = true;
        repository.Update(user);

        var auth = FirebaseAuth.GetAuth(firebaseAppCreator.GetFirebaseApp());
        var firebaseUser = auth.GetUserAsync(user.ExternalId).GetAwaiter().GetResult();
        
        if (firebaseUser == null && !string.IsNullOrEmpty(user.ExternalId2))
            firebaseUser = auth.GetUserAsync(user.ExternalId2).GetAwaiter().GetResult();

        if (firebaseUser != null && !firebaseUser.EmailVerified)
        { 
            auth.UpdateUserAsync(new UserRecordArgs { EmailVerified = true, Uid = user.ExternalId }); 

            if (!string.IsNullOrEmpty(user.ExternalId2))
                auth.UpdateUserAsync(new UserRecordArgs { EmailVerified = true, Uid = user.ExternalId2 });
        }

        return user;
    }

    public async ValueTask<ResponseStatus> CreateUserInFirebase(string email, string password)
    {
        try
        {
            var auth = FirebaseAuth.GetAuth(firebaseAppCreator.GetFirebaseApp());
            var user = auth.CreateUserAsync(new UserRecordArgs { Email = email, Password = password }).GetAwaiter().GetResult();
            var url = auth.GenerateEmailVerificationLinkAsync(email).GetAwaiter().GetResult();

            await SendEmailByAmazonSesAsync(new EmailInput
            {
                To = email,
                Subject = "TherExNow email verification (do not reply)",
                Content = "click on this link to verify your email: " + "<br>" + url,
            }, null); 

            return ResponseStatus.Success;
        }
        catch
        {
            return ResponseStatus.UnknownError;
        }
    }

    public async ValueTask<ResponseStatus> ForgotPassword(string email)
    {
        var entity = repository.Where(a => a.Email == email.Trim()).FirstOrDefault();
        if (entity is null)
            return ResponseStatus.NotFound;

        try
        {
            var auth = FirebaseAuth.GetAuth(firebaseAppCreator.GetFirebaseApp());
            var url = auth.GeneratePasswordResetLinkAsync(entity.Email).GetAwaiter().GetResult();

            await SendEmailByAmazonSesAsync(new EmailInput
            {
                To = entity.Email,
                Subject = "Forgot Password",
                Content = @$"Hi {entity.FullName}, <br>
                             There was a request to change your password! <br>
                             If you did not make this request then please ignore this email. <br>
                             Otherwise, please click this link to change your password: <br> <a href='{url}'> {url} </a>"
            }, null);

            return ResponseStatus.Success;
        }
        catch
        {
            return ResponseStatus.NotFound;
        }
    }

    public ResponseBase<EmailVerifiedDto> GetEmailVerified(string userIdInFirebase)
    {
        var result = new EmailVerifiedDto 
        {
            VerifiedInDatabase = repository.Any(a => (a.ExternalId == userIdInFirebase || a.ExternalId2 == userIdInFirebase) && a.IsConfirmed)
        };

        var auth = FirebaseAuth.GetAuth(firebaseAppCreator.GetFirebaseApp());
        var firebaseUser = auth.GetUserAsync(userIdInFirebase).GetAwaiter().GetResult();
        if (firebaseUser != null && firebaseUser.EmailVerified)
            result.VerifiedInFirebase = true;

        return result;
    }

    public void AddUserActiveDay(int userId)
    {
        var today = DateTime.UtcNow.Date;
        if (!repository.Any<UserActiveDay>(a => a.UserId == userId && a.Day == today))
            repository.Add(new UserActiveDay
            {
                Day = today,
                UserId = userId,
            });
    }
    #endregion
}

public class SendGridConfig
{
    public string ApiKey { get; set; }
    public string EmailFrom { get; set; }
}

public class TwilioConfig
{
    public string AccountSID { get; set; }
    public string AuthToken { get; set; }
    public string FromNumber { get; set; }
}
