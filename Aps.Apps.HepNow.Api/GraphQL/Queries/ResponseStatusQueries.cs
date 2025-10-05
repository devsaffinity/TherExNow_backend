namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
public class ResponseStatusQueries
{
    [GraphQLName("responseStatus_getAll")]
    public IEnumerable<ResponseStatus> GetAll()
    {
        var result = new[] {
            ResponseStatus.GetAll<ResponseStatus>() ,
            ResponseStatus.GetAll<CustomResponseStatus>()
        }.SelectMany(x => x);
        return result;
    }
}
