namespace Aps.Apps.HepNow.Models.Inputs;

public class SubCategoryInput : InputDef
{
    public int? Id { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; }
}
