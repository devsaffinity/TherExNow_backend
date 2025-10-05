namespace Aps.Apps.HepNow.Models.Inputs;

public class ProductInput: InputDef
{
    public int? Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string PhotoUrl { get; set; }

    public string OtherMediaUrl { get; set; }

    public string Colors { get; set; }
    
    public decimal? Price { get; set; }

    public bool? IsActive { get; set; }
}
