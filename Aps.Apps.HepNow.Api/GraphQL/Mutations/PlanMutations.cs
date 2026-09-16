using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(PlanMutations))]
public class PlanMutations : MutationBase<Plans, PlanInput, IPlanService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service(ServiceKind.Default)] IPlanService service, int entityId) => null;

    [GraphQLName("plan_createPlan")]
    public ResponseBase<Plans> Create([Authentication] Authentication authentication, [Service] IPlanService service, PlanInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        input.Title = input.Title.Trim();

        if (service.Any(a => a.Title == input.Title).Status == ResponseStatus.Success)
            return CustomResponseStatus.DuplicateName;

        return service.Add(input);
    }

    [GraphQLName("plan_updatePlan")]
    public ResponseBase<Plans> Update([Authentication] Authentication authentication, [Service] IPlanService service, PlanInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        if (!string.IsNullOrEmpty(input.Title))
        {
            input.Title = input.Title.Trim();
            if (service.Any(a => a.Title == input.Title && a.Id != input.Id).Status == ResponseStatus.Success)
                return CustomResponseStatus.DuplicateName;
        }

        return service.Update(input);
    }

    [GraphQLName("plan_deletePlan")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] IPlanService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Remove(entityId).Status;
    }
    #endregion
}
