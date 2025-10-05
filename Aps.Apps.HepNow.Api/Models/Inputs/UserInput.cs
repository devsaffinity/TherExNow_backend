namespace Aps.Apps.HepNow.Models.Inputs;

public class UserInput: InputDef
{
	public int? Id { get; set; }

	public string Language { get; set; }

	public string FullName { get; set; }

	public Gender? Gender { get; set; }

	public string Occupation { get; set; }

	public string Address { get; set; }

	public int? YearsOfExperience { get; set; }

	public string City { get; set; }

	public string State { get; set; }

	public string ZipCode { get; set; }

	public string PhoneNumber { get; set; }

	public PlatformType? PlatformType { get; set; }

	public UserType? UserType { get; set; }

	public string PhotoUrl { get; set; }

	public string ConfirmUrl { get; set; }

	public string Email { get; set; }

    public bool? IsSocial { get; set; }

    public string CountryCode { get; set; }

    public string Country { get; set; }
}
