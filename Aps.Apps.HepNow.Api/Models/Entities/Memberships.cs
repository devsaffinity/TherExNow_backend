using System.ComponentModel.DataAnnotations.Schema;

namespace Aps.Apps.HepNow.Models.Entities;

public class Memberships: EntityDef
{
	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public decimal PaymentAmount { get; set; }

	public Users User { get; set; }

	public Plans Plan { get; set; }

	public int UserId { get; set; }

	public int PlanId { get; set; }
}
