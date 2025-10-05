namespace Aps.Apps.HepNow.Models.Inputs;

public class RoutineInput: InputDef
{
    public int? Id { get; set; }

    public string Name { get; set; }

	public string Description { get; set; }

    [GraphQLIgnore]
	public int? UserId { get; set; }

	public string FileUrl { get; set; }

	public List<RoutineExerciseInput> RoutineExercises { get; set; }
}
