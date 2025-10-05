using System.ComponentModel.DataAnnotations.Schema;

namespace Aps.Apps.HepNow.Models.Entities;

public class Products: EntityDef
{
	public string Name { get; set; }

	public string Description { get; set; }

	public string PhotoUrl { get; set; }

	public string OtherMediaUrl { get; set; }

	public decimal Price { get; set; }

	public string Colors { get; set; }

	public bool? IsActive { get; set; }

	public ICollection<ExerciseProducts> ExerciseProducts { get; set; }

	public ICollection<RequestProducts> RequestProducts { get; set; }
}
