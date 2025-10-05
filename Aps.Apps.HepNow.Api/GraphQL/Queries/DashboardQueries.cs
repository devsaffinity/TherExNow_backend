using Aps.Apps.HepNow.Models.Dtos;
using Aps.CommonBack.Base.Attributes;
namespace Aps.Apps.HepNow.GraphQL.Queries;

[ExtendObjectType(typeof(Query))]
[GraphBase(nameof(DashboardQueries))]
public class DashboardQueries
{
    #region functions
    [GraphQLName("dashboard_getTodayRevenueAndActiveUsers")]
    public ResponseBase<DashboardTodayRevenueAndActiveUserDto> GetTodayRevenueAndActiveUsers([Authentication] Authentication authentication
        , [Service] IDashboardService service)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.GetTodayRevenueAndActiveUsers();
    }

    [GraphQLName("dashboard_getActiveUsers")]
    public ListResponseBase<DashboardActiveUserDto> GetActiveUsers([Authentication] Authentication authentication
        , [Service] IDashboardService service, DateTime startDate, DateTime endDate)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.GetActiveUsers(startDate, endDate);
    }

    [GraphQLName("dashboard_getRevenuePlan")]
    public ListResponseBase<DashboardRevenuePlanDto> GetRevenuePlan([Authentication] Authentication authentication
        , [Service] IDashboardService service, DateTime startDate, DateTime endDate)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.GetRevenuePlan(startDate, endDate);
    }

    [GraphQLName("dashboard_getRevenueShop")]
    public ListResponseBase<DashboardRevenueShopDto> GetRevenueShop([Authentication] Authentication authentication
        , [Service] IDashboardService service, DateTime startDate, DateTime endDate)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.GetRevenueShop(startDate, endDate);
    }

    [GraphQLName("dashboard_getExerciseInCategory")]
    public ListResponseBase<DashboardExerciseInCategoryDto> GetExerciseInCategory([Authentication] Authentication authentication
        , [Service] IDashboardService service)
    {
        if (authentication.Status != ResponseStatus.Success)
            return authentication.Status;

        if (authentication.CurrentUser.UserType != UserType.Admin)
            return CustomResponseStatus.CurrentUserIsNotAdmin;

        return service.GetExerciseInCategory();
    }
    #endregion
}
