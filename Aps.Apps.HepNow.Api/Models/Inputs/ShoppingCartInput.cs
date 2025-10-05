namespace Aps.Apps.HepNow.Models.Inputs;

public class ShoppingCartInput: InputDef
{
	public int? Id { get; set; }

	public int Count { get; set; }

	public string Color { get; set; }

	public int ProductId { get; set; }

    [GraphQLIgnore]
	public int UserId { get; set; }
}
