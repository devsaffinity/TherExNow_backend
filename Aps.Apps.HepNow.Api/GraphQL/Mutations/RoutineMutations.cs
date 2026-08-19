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
    public async Task<ResponseBase<Routines>> Create([Authentication] Authentication authentication, [Service] IRoutineService service, RoutineInput input
        , [Service] INotificationService notificationService
        , [Service] IMembershipService membershipService)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (!await membershipService.CheckMaxTryFreePlan(authentication.CurrentUser.Id, NotificationType.MaxTryFreePlanForRoutine))
            return CustomResponseStatus.MaxTryFreePlanForRoutine;

        input.UserId = authentication.CurrentUser.Id;
        input.Name = input.Name.Trim();

        if (service.Any(a => a.UserId == authentication.CurrentUser.Id && a.Name == input.Name).Status == ResponseStatus.Success)
            return CustomResponseStatus.DuplicateName;

        input.RoutineExercises = DeduplicateByExerciseId(input.RoutineExercises);

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

        input.RoutineExercises = DeduplicateByExerciseId(input.RoutineExercises);

        return service.Update(input);
    }

    private static List<RoutineExerciseInput> DeduplicateByExerciseId(List<RoutineExerciseInput> routineExercises)
    {
        if (routineExercises is null)
            return null;

        return routineExercises
            .GroupBy(e => e.ExerciseId)
            .Select(g => g.First())
            .ToList();
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
