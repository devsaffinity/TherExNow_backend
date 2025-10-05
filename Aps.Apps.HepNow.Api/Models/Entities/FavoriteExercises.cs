namespace Aps.Apps.HepNow.Models.Entities;

public class FavoriteExercises: EntityDef
{
	public Users User { get; set; }

	public Exercises Exercise { get; set; }

	public int UserId { get; set; }

	public int ExerciseId { get; set; }
}
