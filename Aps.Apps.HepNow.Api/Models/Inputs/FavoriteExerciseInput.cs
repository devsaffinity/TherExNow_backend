namespace Aps.Apps.HepNow.Models.Inputs;

public class FavoriteExerciseInput: InputDef
{
    [GraphQLIgnore]
	public int UserId { get; set; }

	public int ExerciseId { get; set; }
}
