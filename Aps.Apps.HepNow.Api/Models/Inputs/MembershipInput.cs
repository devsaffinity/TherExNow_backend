namespace Aps.Apps.HepNow.Models.Inputs;

public class MembershipInput: InputDef
{
    [GraphQLIgnore]
	public DateTime StartDate { get; set; }

	[GraphQLIgnore]
	public DateTime EndDate { get; set; }

	public decimal? PaymentAmount { get; set; }

	[GraphQLIgnore]
	public int UserId { get; set; }

	public int PlanId { get; set; }

    [GraphQLIgnore]
	public bool IsFree { get; set; }
}
