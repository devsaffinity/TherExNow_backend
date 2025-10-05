using Aps.CommonBack.Base.Models.Dtos;

namespace Aps.Apps.HepNow.Models.Dtos
{
    public class DashboardRevenuePlanDto : DtoDef
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
