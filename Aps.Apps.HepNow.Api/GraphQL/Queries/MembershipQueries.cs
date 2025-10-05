using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(MembershipQueries))]

public class MembershipQueries : QueryBase<Memberships, MembershipInput, IMembershipService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Memberships> Get(ClaimsPrincipal claims, [Service] IMembershipService membershipService, int membershipId) => null;

    [GraphQLIgnore]
    public override ListResponseBase<Memberships> GetItems(ClaimsPrincipal claims, [Service] IMembershipService membershipService) => null;

    [GraphQLName("membership_getCurrentMembership")]
    public SingleResponseBase<Memberships> GetCurrentMembership([Authentication] Authentication authentication, [Service] IMembershipService membershipService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return membershipService.GetCurrentMembership(authentication.CurrentUser.Id);
    }
    #endregion
}
