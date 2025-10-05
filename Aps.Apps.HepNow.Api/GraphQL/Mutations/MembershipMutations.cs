using Aps.Apps.HepNow.Models.Dtos;
using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(MembershipMutations))]
public class MembershipMutations: MutationBase<Memberships,MembershipInput,IMembershipService,Users>
{
    #region functions
    public async Task<ResponseBase<Memberships>> Create([Authentication] Authentication authentication
        , [Service] IMembershipService service
        , [Service] INotificationService notificationService
        , MembershipInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        input.UserId = authentication.CurrentUser.Id;
        var response = service.Add(input);
        if (response.Status == ResponseStatus.Success)
        {
            NotificationInput notificationInput = new()
            {
                NotificationType = response.Result.PaymentAmount == 0 ? NotificationType.FreePlanPurchased : NotificationType.PlanUpgraded,
                UserId = response.Result.UserId,
            };
            await notificationService.Send(notificationInput);
        }
        return response;
    }

    [GraphQLIgnore]
    public override ResponseBase<Memberships> Update(ClaimsPrincipal claims, [Service] IMembershipService service, MembershipInput input) => null;

    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service] IMembershipService service, int entityId) => null;

    [GraphQLName("membership_createIntentForPayment")]
    public ResponseBase<IntentDto> CreateIntentForPayment(ClaimsPrincipal claims, [Service] IUserService userService, [Service] IMembershipService membershipService, double amount)
    {
        var user = userService.GetUser(claims);
        if (user.Status != ResponseStatus.Success)
        {
            return user.Status;
        }

        return membershipService.CreateIntentForPayment(user.Result.Email, amount);
    }

    [GraphQLName("membership_createSubscription")]
    public async Task<ResponseBase<string>> CreateSubscription([Authentication] Authentication authentication, [Service] IMembershipService _membershipService, int planId, bool forYear)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return await _membershipService.CreateSubscription(planId, authentication.CurrentUser, forYear);
    }

    [GraphQLName("membership_cancelSubscription")]
    public async Task<ResponseStatus> CancelSubscription([Authentication] Authentication authentication, [Service] IMembershipService _membershipService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return await _membershipService.CancelSubscription(authentication.CurrentUser);
    }

    #endregion
}
