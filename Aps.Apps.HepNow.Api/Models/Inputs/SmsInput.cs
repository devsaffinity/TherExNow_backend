namespace Aps.Apps.HepNow.Models.Inputs
{
    public class SmsInput: InputDef
    {
        public string PhoneNumber { get; set; }
        public string Text { get; set; }
    }
}
