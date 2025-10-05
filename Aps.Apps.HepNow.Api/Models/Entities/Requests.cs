namespace Aps.Apps.HepNow.Models.Entities;

public class Requests: EntityDef
{
	public string Address { get; set; }

    public string CountryCode { get; set; }

    public string City { get; set; }

	public string State { get; set; }

	public string ZipCode { get; set; }

	public RequestStatus RequestStatus { get; set; }

	public ICollection<RequestProducts> RequestProducts { get; set; }
	
	public ICollection<Notification> Notifications { get; set; }

	public Users User { get; set; }

	public int UserId { get; set; }
}
