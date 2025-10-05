namespace Aps.Apps.HepNow.Models.Entities
{
    public class AppSettings: EntityDef
    {
        public string PrivacyPolicy { get; set; }
        public string TermsAndConditions { get; set; }
        public decimal? ShippingCost { get; set; }
    }
}
