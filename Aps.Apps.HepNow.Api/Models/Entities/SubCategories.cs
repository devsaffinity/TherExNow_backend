namespace Aps.Apps.HepNow.Models.Entities;

public class SubCategories : EntityDef
{
    public string Name { get; set; }

    public int CategoryId { get; set; }

    public Categories Category { get; set; }

    public ICollection<Exercises> Exercises { get; set; }
}
