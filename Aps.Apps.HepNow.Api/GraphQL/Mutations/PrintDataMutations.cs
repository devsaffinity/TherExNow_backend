using Aps.CommonBack.Base.Attributes;

namespace Aps.Apps.HepNow.GraphQL.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class PrintDataMutations
    {
        [GraphQLName("printData_createPrintData")]
        public ResponseBase<PrintData> Create([Authentication] Authentication authentication, [Service] IPrintDataService service, PrintDataInput input)
        {
            if (authentication.Status != ResponseStatus.Success)
                return authentication.Status;

            input.UserId = authentication.CurrentUser.Id;
            return service.Add(input);
        }
    }
}
