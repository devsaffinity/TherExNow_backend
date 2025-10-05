namespace Aps.Apps.HepNow.Models.Inputs;

public class RequestInput: InputDef
{
	public string Address { get; set; }

    public string CountryCode { get; set; }

    public string City { get; set; }

	public string State { get; set; }

	public string ZipCode { get; set; }

	[GraphQLIgnore]
	public RequestStatus? RequestStatus { get; set; }

	[GraphQLIgnore]
	public int? UserId { get; set; }

	public List<RequestProductInput> RequestProducts { get; set; }
}
