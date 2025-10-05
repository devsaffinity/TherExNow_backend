using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Services;

public class DashboardService : ServiceBase<EntityDef, InputDef>, IDashboardService
{
    public DashboardService(IUserRepository userRepository
        , IMembershipRepository membershipRepository
        , IRequestProductRepository requestProductRepository
        , IExerciseRepository exerciseRepository) : base(null)
    {
        this.userRepository = userRepository;
        this.membershipRepository = membershipRepository;
        this.requestProductRepository = requestProductRepository;
        this.exerciseRepository = exerciseRepository;
    }

    #region props
    private readonly IUserRepository userRepository;
    private readonly IMembershipRepository membershipRepository;
    private readonly IRequestProductRepository requestProductRepository;
    private readonly IExerciseRepository exerciseRepository;
    #endregion

    #region functions
    public ResponseBase<DashboardTodayRevenueAndActiveUserDto> GetTodayRevenueAndActiveUsers()
    {
        var result = new DashboardTodayRevenueAndActiveUserDto
        {
            RevenuePlan = membershipRepository.Where(m => m.CreatedDate.Date == DateTime.UtcNow.Date).Sum(m => m.PaymentAmount),
            RevenueShop = requestProductRepository.Where(m => m.CreatedDate.Date == DateTime.UtcNow.Date).Sum(m => m.Price * m.Count),
            ActiveUser = userRepository.Where<UserActiveDay>(a => a.Day == DateTime.UtcNow.Date).Count(),
        };

        return result; 
    }

    public ListResponseBase<DashboardActiveUserDto> GetActiveUsers(DateTime startDate, DateTime endDate)
    {
        var result = userRepository.GetDbSet<UserActiveDay>()
            .Where(u => u.Day >= startDate.Date && u.Day <= endDate.Date)
            .GroupBy(u => u.Day).Select(u => new DashboardActiveUserDto 
        {
            Date = u.Key,
            Count = u.Count(),
        });

        return ListResponseBase<DashboardActiveUserDto>.Success(result);
    }

    public ListResponseBase<DashboardRevenuePlanDto> GetRevenuePlan(DateTime startDate, DateTime endDate)
    {
        var result = membershipRepository.GetDbSet()
            .Where(u => u.CreatedDate.Date >= startDate.Date && u.CreatedDate.Date <= endDate.Date)
            .GroupBy(u => u.CreatedDate.Date).Select(u => new DashboardRevenuePlanDto
            {
                Date = u.Key,
                Amount = u.Sum(a => a.PaymentAmount),
            });

        return ListResponseBase<DashboardRevenuePlanDto>.Success(result);
    }

    public ListResponseBase<DashboardRevenueShopDto> GetRevenueShop(DateTime startDate, DateTime endDate)
    {
        var result = requestProductRepository.GetDbSet()
            .Where(u => u.CreatedDate.Date >= startDate.Date && u.CreatedDate.Date <= endDate.Date)
            .GroupBy(u => u.CreatedDate.Date).Select(u => new DashboardRevenueShopDto
            {
                Date = u.Key,
                Amount = u.Sum(p => p.Count * p.Price),
            });

        return ListResponseBase<DashboardRevenueShopDto>.Success(result);
    }

    public ListResponseBase<DashboardExerciseInCategoryDto> GetExerciseInCategory()
    {
        var result = exerciseRepository.GetDbSet().GroupBy(e => e.SubCategory.CategoryId).Select(e => new DashboardExerciseInCategoryDto
        {
            CategoryId = e.Key,
            CategoryName = e.FirstOrDefault().SubCategory.Category.Name,
            Count = e.Count()
        });

        return ListResponseBase<DashboardExerciseInCategoryDto>.Success(result);
    }
    #endregion
}
