namespace Aps.Apps.HepNow.Models.Entities
{
    public class WebhookLog: EntityDef
    {
        public MembershipProvider Provider { get; set; }
        public string Data { get; set; }
        public string Exception { get; set; }
    }
}
