namespace Aps.Apps.HepNow.Models.Inputs;

public class CustomizedExerciseInput: InputDef
{
	public string Note { get; set; }

	public int? Reps { get; set; }

	public int? Hold { get; set; }

	public HoldType? HoldType { get; set; }

	public int? Set { get; set; }

	public int? Perform { get; set; }

	public PerformType? PerformType { get; set; }

	public int ExerciseId { get; set; }

    public string OtherMediaUrl { get; set; }
}
