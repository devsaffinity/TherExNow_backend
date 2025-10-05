namespace Aps.Apps.HepNow.Models.Entities;

public class ShoppingCart : EntityDef
{
	public int Count { get; set; }

	public string Color { get; set; }

	public Users User { get; set; }

	public Products Product { get; set; }

	public int UserId { get; set; }

	public int ProductId { get; set; }
}
