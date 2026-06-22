using Aps.CommonBack.Base.Attributes;
using Aps.CommonBack.Base.Extensions;
using System.Reflection;

namespace Aps.Apps.HepNow.GraphQL.ObjectTypes;
public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        var allMutations = AppDomain.CurrentDomain.GetAssemblies()
                   .SelectMany(t => t.GetTypes())
                   .Where(t => t.IsClass && t.Namespace == typeof(Mutation).Namespace);

        foreach (var mutation in allMutations)
        {
            if (mutation.CustomAttributes.Any(a => a.AttributeType == typeof(GraphBaseAttribute)))
            {
                var entityName = mutation.CustomAttributes.First(a => a.AttributeType == typeof(GraphBaseAttribute))
                    .ConstructorArguments[0].Value.ToString()
                    .Replace("Mutations", "");

                foreach (var methodType in Enum.GetValues(typeof(MethodTypes)).Cast<MethodTypes>()
                    .Where(a => !a.ToString().StartsWith("Get")))
                {
                    MethodInfo mi = mutation.GetMethods().FirstOrDefault(a => a.Name == methodType.ToString());
                    if (mi.CustomAttributes.Any(a => a.AttributeType == typeof(GraphQLIgnoreAttribute)))
                        continue;

                    descriptor.Field(mi)
                        .Name(GetMethodName(entityName, methodType));
                }
            }
        }
    }

    private string GetMethodName(string entityName, MethodTypes methodType)
    {
        string simpleName = entityName.FirstCharToLower();
        if (methodType == MethodTypes.Update)
        {
            return $"{simpleName}_update{simpleName.FirstCharToUpper()}";
        }
        if (methodType == MethodTypes.Delete)
        {
            return $"{simpleName}_delete{simpleName.FirstCharToUpper()}";
        }
        return $"{simpleName}_create{simpleName.FirstCharToUpper()}";
    }
}
