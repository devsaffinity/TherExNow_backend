using Aps.CommonBack.Base.Models.Dtos;

namespace Aps.Apps.HepNow.Models.Dtos
{
    public class DashboardActiveUserDto: DtoDef
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }
}
