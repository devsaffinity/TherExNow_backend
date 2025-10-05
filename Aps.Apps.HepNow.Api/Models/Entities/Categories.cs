namespace Aps.Apps.HepNow.Models.Entities;

public class Categories : EntityDef
{
    public string Name { get; set; }

    public int Order { get; set; }

    public ICollection<SubCategories> SubCategories { get; set; }
}
