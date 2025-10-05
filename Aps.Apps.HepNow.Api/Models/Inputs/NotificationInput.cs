namespace Aps.Apps.HepNow.Models.Inputs
{
    public class NotificationInput: InputDef
    {
        public int UserId { get; set; }
        public NotificationType NotificationType { get; set; }
        public int? RequestId { get; set; }
        public int? ExerciseId { get; set; }
        public string Description { get; set; }
    }
}
