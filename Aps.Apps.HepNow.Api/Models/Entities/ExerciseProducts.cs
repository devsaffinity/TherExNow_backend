namespace Aps.Apps.HepNow.Models.Entities;

public class ExerciseProducts: EntityDef
{
	public Exercises Exercise { get; set; }

	public Products Product { get; set; }

	public int ExerciseId { get; set; }

	public int ProductId { get; set; }
}
