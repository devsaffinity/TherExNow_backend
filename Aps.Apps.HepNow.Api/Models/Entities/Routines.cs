namespace Aps.Apps.HepNow.Models.Entities;

public class Routines: EntityDef
{
	public string Name { get; set; }

	public string Description { get; set; }

	public ICollection<RoutineExercises> RoutineExercises { get; set; }

	public Users User { get; set; }

	public int UserId { get; set; }

	public string FileUrl { get; set; }
}
