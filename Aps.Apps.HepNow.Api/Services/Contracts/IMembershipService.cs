using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface IMembershipService: IServiceBase<Memberships, MembershipInput>
{
    SingleResponseBase<Memberships> GetCurrentMembership(int userId);
    Task<ResponseBase<bool>> CheckMaxTryFreePlan(int userId, NotificationType notificationType);
    ResponseBase AddFreePlan(int userId);
    ResponseBase<Memberships> GrantComplimentaryPlan(int userId, int planId, int months);
    ResponseBase<IntentDto> CreateIntentForPayment(string email, double amount);
    // `claims` lets the caller hand over the already-resolved principal. Resolvers run inside
    // HotChocolate's own execution scope, and this codebase does not trust IHttpContextAccessor
    // there -- HttpRequestInterceptor deliberately stashes HttpContext in GraphQL global state
    // instead. Passing it explicitly removes the ambient dependency for the one thing that needs
    // it: recovering a billing address for an account stored without one.
    Task<ResponseBase<string>> CreateSubscription(Users user, bool forYear, string successUrl, string cancelUrl, ClaimsPrincipal claims = null);
    Task<ResponseStatus> CancelSubscription(Users user);
    Task<ResponseBase<Memberships>> VerifyReceiptApple(int userId, string base64Receipt);
    Task<ResponseBase<Memberships>> VerifyReceiptGoogle(int userId, string packageName, string productId, string purchaseToken);
}
