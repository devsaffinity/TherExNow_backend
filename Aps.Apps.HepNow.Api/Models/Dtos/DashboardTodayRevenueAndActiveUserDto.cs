using Aps.CommonBack.Base.Models.Dtos;

namespace Aps.Apps.HepNow.Models.Dtos
{
    public class DashboardTodayRevenueAndActiveUserDto: DtoDef
    {
        public decimal RevenuePlan { get; set; }
        public decimal RevenueShop { get; set; }
        public int ActiveUser { get; set; }
    }
}
