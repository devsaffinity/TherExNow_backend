using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services.Contracts;

public interface IDashboardService: IServiceBase<EntityDef, InputDef>
{
    ResponseBase<DashboardTodayRevenueAndActiveUserDto> GetTodayRevenueAndActiveUsers();
    ListResponseBase<DashboardActiveUserDto> GetActiveUsers(DateTime startDate, DateTime endDate);
    ListResponseBase<DashboardRevenuePlanDto> GetRevenuePlan(DateTime startDate, DateTime endDate);
    ListResponseBase<DashboardRevenueShopDto> GetRevenueShop(DateTime startDate, DateTime endDate);
    ListResponseBase<DashboardExerciseInCategoryDto> GetExerciseInCategory();
}
