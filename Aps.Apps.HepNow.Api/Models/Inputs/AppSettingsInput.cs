namespace Aps.Apps.HepNow.Models.Inputs
{
    public class AppSettingsInput: InputDef
    {
        [GraphQLIgnore]
        public int? Id { get; set; }
        public string PrivacyPolicy { get; set; }
        public string TermsAndConditions { get; set; }
		public decimal? ShippingCost { get; set; }
	}
}
