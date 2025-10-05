namespace Aps.Apps.HepNow.Models.Inputs;

public class RequestUpdateInput: InputDef
{
	public int Id { get; set; }

	public RequestStatus? RequestStatus { get; set; }
}
