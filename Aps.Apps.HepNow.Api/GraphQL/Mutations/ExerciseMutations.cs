using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(ExerciseMutations))]
public class ExerciseMutations: MutationBase<Exercises,ExerciseInput,IExerciseService,Users>
{
    #region functions
    [GraphQLName("exercise_createExercise")]
    public ResponseBase<Exercises> Create([Authentication] Authentication authentication, [Service] IExerciseService service, [Service] ILogger<ExerciseMutations> logger, ExerciseInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        input.Name = input.Name.Trim();

        if (service.Any(a => a.Name == input.Name).Status == ResponseStatus.Success)
            logger.LogWarning("Exercise created with a duplicate name: {Name}", input.Name);

        return service.Add(input);
    }

    [GraphQLName("exercise_updateExercise")]
    public ResponseBase<Exercises> Update([Authentication] Authentication authentication, [Service] IExerciseService service, [Service] ILogger<ExerciseMutations> logger, ExerciseInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        if (!string.IsNullOrEmpty(input.Name))
        {
            input.Name = input.Name.Trim();
            if (service.Any(a => a.Name == input.Name && a.Id != input.Id).Status == ResponseStatus.Success)
                logger.LogWarning("Exercise updated to a duplicate name: {Name}", input.Name);
        }

        return service.Update(input);
    }

    [GraphQLName("exercise_deleteExercise")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] IExerciseService service, int entityId, string description)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Remove(entityId, description).Status;
    }
    #endregion
}
