namespace Aps.Apps.HepNow.Services.Contracts;

/// <summary>Firebase Authentication records, addressed by uid.</summary>
public interface IFirebaseAccounts
{
    /// <summary>Whether Firebase still has this uid. Throws when Firebase cannot be asked.</summary>
    Task<bool> ExistsAsync(string uid);

    /// <summary>Deletes the uid. A uid Firebase no longer has counts as deleted.</summary>
    Task DeleteAsync(string uid);
}
