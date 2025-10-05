namespace Aps.Apps.HepNow.Models.Inputs;

public class PlanInput: InputDef
{
    public int? Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public decimal? PriceMonth { get; set; }

    public decimal? PriceYear { get; set; }

    public string Features { get; set; }
}
