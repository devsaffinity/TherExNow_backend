namespace Aps.Apps.HepNow.Models.Inputs;

public class RequestProductInput: InputDef
{
	public int Count { get; set; }

    [GraphQLIgnore]
	public decimal Price { get; set; }

	public string Color { get; set; }

	public int ProductId { get; set; }
}
