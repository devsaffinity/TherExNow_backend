using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(RoutineMutations))]
public class RoutineMutations : MutationBase<Routines, RoutineInput, IRoutineService, Users>
{
    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Routines> Create(ClaimsPrincipal claims, [Service] IRoutineService service, RoutineInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase<Routines> Update(ClaimsPrincipal claims, [Service] IRoutineService service, RoutineInput input) => null;
    [GraphQLIgnore]
    public override ResponseBase Delete(ClaimsPrincipal claims, [Service] IRoutineService service, int entityId) => null;

    [GraphQLName("routine_createRoutine")]
    public ResponseBase<Routines> Create([Authentication] Authentication authentication, [Service] IRoutineService service, RoutineInput input
        , [Service] INotificationService notificationService
        , [Service] IMembershipService membershipService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (!membershipService.CheckMaxTryFreePlan(authentication.CurrentUser.Id, OptionsForMaxTryFreePlan.Routine).Result)
        {
            if (notificationService.GetAll(a => a.UserId == authentication.CurrentUser.Id && a.NotificationType == NotificationType.MaxTryFreePlanForRoutine).Result.Count() == 0)
                notificationService.Send(new NotificationInput
                {
                    UserId = authentication.CurrentUser.Id,
                    NotificationType = NotificationType.MaxTryFreePlanForRoutine,
                });

            return CustomResponseStatus.MaxTryFreePlanForRoutine;
        }

        input.UserId = authentication.CurrentUser.Id;
        input.Name = input.Name.Trim();

        if (service.Any(a => a.UserId == authentication.CurrentUser.Id && a.Name == input.Name).Status == ResponseStatus.Success)
            return CustomResponseStatus.DuplicateName;

        return service.Add(input);
    }

    [GraphQLName("routine_updateRoutine")]
    public ResponseBase<Routines> Update([Authentication] Authentication authentication, [Service] IRoutineService service, RoutineInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (!string.IsNullOrEmpty(input.Name))
        {
            input.Name = input.Name.Trim();
            if (service.Any(a => a.UserId == authentication.CurrentUser.Id && a.Name == input.Name && a.Id != input.Id).Status == ResponseStatus.Success)
                return CustomResponseStatus.DuplicateName;
        }
        return service.Update(input);
    }

    [GraphQLName("routine_deleteRoutine")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] IRoutineService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        return service.Remove(entityId).Status;
    }
    #endregion
}
