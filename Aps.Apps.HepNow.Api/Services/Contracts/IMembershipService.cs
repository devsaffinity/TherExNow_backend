using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface IMembershipService: IServiceBase<Memberships, MembershipInput>
{
    SingleResponseBase<Memberships> GetCurrentMembership(int userId);
    Task<ResponseBase<bool>> CheckMaxTryFreePlan(int userId, NotificationType notificationType);
    ResponseBase AddFreePlan(int userId);
    ResponseBase<IntentDto> CreateIntentForPayment(string email, double amount);
    Task<ResponseBase<string>> CreateSubscription(Users user, bool forYear, string successUrl, string cancelUrl);
    Task<ResponseStatus> CancelSubscription(Users user);
    Task<ResponseBase<Memberships>> VerifyReceiptApple(int userId, string base64Receipt);
    Task<ResponseBase<Memberships>> VerifyReceiptGoogle(int userId, string packageName, string productId, string purchaseToken);
}
