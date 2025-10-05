using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Reflection;

namespace Aps.Apps.HepNow.Extensions;

public static class ModelBuilderExtensions
{
    public static void RegisterAllEntities<IType>(this ModelBuilder builder)
    {
        var types = Assembly.GetExecutingAssembly().GetTypes().Where(s => s.IsClass && !s.IsAbstract && s.IsPublic && s.IsSubclassOf(typeof(IType)));

        foreach (var type in types)
        {
            // On Model Creating
            builder.Entity(type);
        }

    }

    public static void SoftDeleteGolobalFilter(this ModelBuilder modelBuilder)
    {
        Expression<Func<EntityDef, bool>> filterExpr = bm => !bm.IsDeleted;
        foreach (var mutableEntityType in modelBuilder.Model.GetEntityTypes())
        {
            if (mutableEntityType.ClrType.IsAssignableTo(typeof(EntityDef)))
            {
                var parameter = Expression.Parameter(mutableEntityType.ClrType);
                var body = ReplacingExpressionVisitor.Replace(filterExpr.Parameters.First(), parameter, filterExpr.Body);
                var lambdaExpression = Expression.Lambda(body, parameter);

                // set filter
                mutableEntityType.SetQueryFilter(lambdaExpression);
            }
        }
    }
}
