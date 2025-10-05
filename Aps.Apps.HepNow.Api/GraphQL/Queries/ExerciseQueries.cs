using Aps.Apps.HepNow.Models.Dtos;
using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(ExerciseQueries))]
public class ExerciseQueries: QueryBase<Exercises,ExerciseInput,IExerciseService,Users>
{

    #region functions
    [GraphQLIgnore]
    public override ResponseBase<Exercises> Get(ClaimsPrincipal claims, [Service] IExerciseService exerciseService, int exerciseId) => null;

    [GraphQLName("exercise_getExercises")]
    public ListResponseBase<ExerciseDto> GetExercises([Authentication] Authentication authentication, [Service] IExerciseService service) 
    {
        return service.GetExercises(authentication.CurrentUser?.Id);
    }

    [GraphQLName("exercise_getProducts")]
    public ListResponseBase<Products> GetProducts([Authentication] Authentication authentication, [Service] IExerciseService service, int exerciseId) 
    {
        return service.GetProducts(exerciseId);
    }
    #endregion
}
