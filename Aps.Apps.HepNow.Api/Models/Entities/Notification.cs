namespace Aps.Apps.HepNow.Models.Entities;

public class Notification: EntityDef
{
	public NotificationType NotificationType { get; set; }

	public Users User { get; set; }

	public int UserId { get; set; }

	public Requests Request { get; set; }

	public int? RequestId { get; set; }

    public bool IsRead { get; set; }

    public bool IsShown { get; set; }

	public string Description { get; set; }

    public Exercises Exercise { get; set; }

    public int? ExerciseId { get; set; }
}
