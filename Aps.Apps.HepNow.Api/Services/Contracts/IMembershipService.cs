using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface IMembershipService: IServiceBase<Memberships, MembershipInput>
{
    SingleResponseBase<Memberships> GetCurrentMembership(int userId);
    ResponseBase<bool> CheckMaxTryFreePlan(int userId, OptionsForMaxTryFreePlan optionsForMaxTryFreePlan);
    ResponseBase AddFreePlan(int userId);
    ResponseBase<IntentDto> CreateIntentForPayment(string email, double amount);
    Task<ResponseBase<string>> CreateSubscription(int planId, Users user, bool forYear);
    Task<ResponseStatus> CancelSubscription(Users user);
}
