using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface IUserService : IUserServiceBase<Users, UserInput>
{
    //ResponseBase<ResponseStatus> SendEmail(EmailInput emailInput, int? userId); //obsolete
    ResponseBase<ResponseStatus> SendSms(SmsInput smsInput, int userId);
    ListResponseBase<UserDto> GetUsers();
    ResponseBase<Users> ConfirmUser(string confirmKey);
    ValueTask<ResponseStatus> CreateUserInFirebase(string email, string password);
    ValueTask<ResponseStatus> ForgotPassword(string email);
    ResponseBase<EmailVerifiedDto> GetEmailVerified(string userIdInFirebase);
    void AddUserActiveDay(int userId);
    ValueTask<ResponseBase<ResponseStatus>> SendEmailByAmazonSesAsync(EmailInput emailInput, int? userId);
}