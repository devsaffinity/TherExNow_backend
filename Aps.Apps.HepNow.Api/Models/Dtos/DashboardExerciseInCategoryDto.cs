using Aps.CommonBack.Base.Models.Dtos;

namespace Aps.Apps.HepNow.Models.Dtos
{
    public class DashboardExerciseInCategoryDto: DtoDef
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int Count { get; set; }
    }
}
