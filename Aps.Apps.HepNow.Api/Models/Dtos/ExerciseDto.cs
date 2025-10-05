using Aps.CommonBack.Base.Models.Dtos;

namespace Aps.Apps.HepNow.Models.Dtos
{
    public class ExerciseDto : DtoDef
    {
        public Exercises Exercise { get; set; }
        public IEnumerable<Products> Products { get; set; }
        public bool IsFavorite { get; set; }
        public int FavoriteCount { get; set; }
    }
}
