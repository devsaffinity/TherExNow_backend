using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
[GraphBase(nameof(ExerciseProductMutations))]
public class ExerciseProductMutations: MutationBase<ExerciseProducts,ExerciseProductInput,IExerciseProductService,Users>
{
    #region functions
    [GraphQLName("exerciseProduct_createExerciseProduct")]
    public ResponseBase<ExerciseProducts> Create([Authentication] Authentication authentication, [Service] IExerciseProductService service, ExerciseProductInput input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Add(input);
    }

    [GraphQLName("exerciseProduct_createExerciseProducts")]
    public ResponseBase<ExerciseProducts> CreateMultiple([Authentication] Authentication authentication, [Service] IExerciseProductService service
        , [Service] IExerciseService exerciseService, [Service] IProductService productService, List<ExerciseProductInput> input)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        foreach(var item in input)
        {
            if (exerciseService.Any(item.ExerciseId).Status != ResponseStatus.Success)
                return CustomResponseStatus.ExerciseNotFound;

            if (productService.Any(item.ProductId).Status != ResponseStatus.Success)
                return CustomResponseStatus.ProductNotFound;

            if (service.GetAll(a => a.ProductId == item.ProductId && a.ExerciseId == item.ExerciseId).Result.Any())
                return ResponseStatus.AlreadyExists;
        }

        foreach (var item in input)
        {
            service.Add(item);
        }

        return ResponseStatus.Success;
    }

    [GraphQLIgnore]
    public override ResponseBase<ExerciseProducts> Update(ClaimsPrincipal claims, [Service] IExerciseProductService service, ExerciseProductInput input) => null;

    [GraphQLName("exerciseProduct_deleteExerciseProduct")]
    public ResponseBase Delete([Authentication] Authentication authentication, [Service] IExerciseProductService service, int entityId)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.Remove(entityId).Status;
    }

    [GraphQLName("exerciseProduct_deleteExerciseProducts")]
    public ResponseBase DeleteMultiple([Authentication] Authentication authentication, [Service] IExerciseProductService service, List<int> entityIdList)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        foreach (var id in entityIdList)
            service.Remove(id);

        return ResponseBase.Success();
    }
    #endregion
}
