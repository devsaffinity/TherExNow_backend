using System.ComponentModel.DataAnnotations.Schema;

namespace Aps.Apps.HepNow.Models.Entities;

public class RequestProducts: EntityDef
{
	public int Count { get; set; }

	public decimal Price { get; set; }

	public string Color { get; set; }

	public Requests Request { get; set; }

	public Products Product { get; set; }

	public int RequestId { get; set; }

	public int ProductId { get; set; }
}
