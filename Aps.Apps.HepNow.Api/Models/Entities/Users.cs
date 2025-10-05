namespace Aps.Apps.HepNow.Models.Entities;

public class Users: UserDef
{
    public PlatformType? PlatformType { get; set; }

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

    public UserType UserType { get; set; }

    public string PhotoUrl { get; set; }

    public int SentSmsCount { get; set; }

    public int SentEmailCount { get; set; }

    public int PrintCount { get; set; }

    public int QrCodeCount { get; set; }

    public bool IsConfirmed { get; set; }

    public string CountryCode { get; set; }

    public string Country { get; set; }

    [GraphQLIgnore]
    public string ConfirmKey { get; set; }

    [GraphQLIgnore]
    public string ExternalId2 { get; set; }

    public ICollection<FavoriteExercises> FavoriteExercises { get; set; }
    
    public ICollection<CustomizedExercise> CustomizedExercises { get; set; }

    public ICollection<Memberships> Memberships { get; set; }

    public ICollection<Requests> Requests { get; set; }

    public ICollection<Routines> Routines { get; set; }

    public ICollection<Notification> Notifications { get; set; }

    public ICollection<ShoppingCart> ShoppingCarts { get; set; }

    public ICollection<UserActiveDay> UserActiveDays { get; set; }

    public string StripeCustomerId { get; set; }
}
