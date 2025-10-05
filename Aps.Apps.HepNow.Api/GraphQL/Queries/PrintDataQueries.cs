using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(PrintDataQueries))]
public class PrintDataQueries
{
    [GraphQLName("printData_getAll")]
    public ListResponseBase<PrintData> GetAll([Authentication] Authentication authentication, [Service] IPrintDataService service)
    {     
        return service.GetAll(predicate: a => true);
    }
}
