namespace Aps.Apps.HepNow.Models.Inputs
{
    public class EmailInput: InputDef
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
    }
}
