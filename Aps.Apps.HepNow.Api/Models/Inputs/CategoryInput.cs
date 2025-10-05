namespace Aps.Apps.HepNow.Models.Inputs;

public class CategoryInput : InputDef
{
    public int? Id { get; set; }

    public string Name { get; set; }

    public int? Order { get; set; }

    public List<SubCategoryForCategoryInput> SubCategories { get; set; }
}

public class SubCategoryForCategoryInput
{
    public int? Id { get; set; }

    public string Name { get; set; }
}
