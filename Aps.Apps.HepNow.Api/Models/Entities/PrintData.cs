namespace Aps.Apps.HepNow.Models.Entities
{
    public class PrintData : EntityDef
    {
        public int UserId { get; set; }
        public string Data { get; set; }

        public Users User { get; set; }
    }
}
