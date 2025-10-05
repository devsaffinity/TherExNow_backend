namespace Aps.Apps.HepNow.Models.Entities;

public class Exercises : EntityDef
{
    public string Name { get; set; }

    public string PhotoUrl { get; set; }

    public string OtherMediaUrl { get; set; }

    public string Note { get; set; }

    public int Reps { get; set; }

    public int Hold { get; set; }

    public int Set { get; set; }

    public int Perform { get; set; }

    public HoldType HoldType { get; set; }

    public PerformType PerformType { get; set; }

    public int SubCategoryId { get; set; }

    public SubCategories SubCategory { get; set; }

    public int FavoriteCount { get; set; }

    public ICollection<ExerciseProducts> ExerciseProducts { get; set; }

    public ICollection<FavoriteExercises> FavoriteExercises { get; set; }

    public ICollection<RoutineExercises> RoutineExercises { get; set; }

    public ICollection<CustomizedExercise> CustomizedExercises { get; set; }

    public ICollection<Notification> Notifications { get; set; }
}
