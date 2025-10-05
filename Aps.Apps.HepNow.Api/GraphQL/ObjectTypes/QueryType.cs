using Aps.CommonBack.Base.Attributes;
using Aps.CommonBack.Base.Extensions;
using System.Reflection;

namespace Aps.Apps.HepNow.GraphQL.ObjectTypes;
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        var allQueries = AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(t => t.GetTypes())
                   .Where(t => t.IsClass && t.Namespace == typeof(Query).Namespace);

        foreach (var query in allQueries)
        {
            if (query.CustomAttributes.Any(a => a.AttributeType == typeof(GraphBaseAttribute)))
            {
                var entityName = query.CustomAttributes.First(a => a.AttributeType == typeof(GraphBaseAttribute))
                    .ConstructorArguments[0].Value.ToString()
                    .Replace("Queries", "");

                foreach (var methodType in Enum.GetValues(typeof(MethodTypes)).Cast<MethodTypes>()
                    .Where(a => a.ToString().StartsWith("Get")))
                {
                    MethodInfo mi = query.GetMethods().FirstOrDefault(a => a.Name == methodType.ToString());
                    if (mi == null || mi.CustomAttributes.Any(a => a.AttributeType == typeof(GraphQLIgnoreAttribute)))
                        continue;

                    descriptor.Field(mi)
                        .Name(GetMethodName(entityName, methodType));
                }
            }
        }
    }

    private NameString GetMethodName(string entityName, MethodTypes methodType)
    {
        if (methodType == MethodTypes.Get)
        {
            return $"{entityName.FirstCharToLower()}_get{entityName}";
        }
        else //for GetItems
        {
            string simpleName;

            if (entityName.EndsWith("y"))
                simpleName = entityName.Substring(0, entityName.Length - 1) + "ies";
            else
                simpleName = entityName + "s";

            return $"{entityName.FirstCharToLower()}_get{simpleName}";
        }
    }
}
