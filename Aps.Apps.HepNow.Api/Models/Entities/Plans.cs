using System.ComponentModel.DataAnnotations.Schema;

namespace Aps.Apps.HepNow.Models.Entities;

public class Plans: EntityDef
{
	public string Title { get; set; }

	public string Description { get; set; }

	public decimal? PriceMonth { get; set; }

	public decimal? PriceYear { get; set; }

	public string Features { get; set; }

	public ICollection<Memberships> Memberships { get; set; }
    
	public string PriceIdYear { get; set; }
	public string PriceIdMonth { get; set; }
}
