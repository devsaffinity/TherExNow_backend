namespace Aps.Apps.HepNow.Models.Entities;

public class RoutineExercises: EntityDef
{
	public string Note { get; set; }

	public int Reps { get; set; }

	public int Hold { get; set; }

	public HoldType HoldType { get; set; }

	public int Set { get; set; }

	public int Perform { get; set; }

	public PerformType PerformType { get; set; }

	public Routines Routine { get; set; }

	public Exercises Exercise { get; set; }

	public int RoutineId { get; set; }

	public int ExerciseId { get; set; }
}
