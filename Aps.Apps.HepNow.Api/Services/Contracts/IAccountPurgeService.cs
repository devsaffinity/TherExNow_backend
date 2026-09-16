namespace Aps.Apps.HepNow.Services.Contracts;

/// <summary>Permanent account deletion. Nothing here soft-deletes.</summary>
public interface IAccountPurgeService
{
    /// <summary>
    /// Deletes the account row <paramref name="userId"/>, every row it owns, and rows signed in with the
    /// same Firebase uid. <paramref name="requestedBy"/> is recorded in the log line, e.g. "self:12".
    /// </summary>
    /// <param name="refusePendingOrders">Return 114 while the account has a Waiting order (self-service).</param>
    /// <param name="refuseAdminTarget">Return 9 when the row is an Admin (admin deleting someone else).</param>
    Task<ResponseStatus> PurgeUserAsync(int userId, string requestedBy, bool refusePendingOrders = false, bool refuseAdminTarget = false);

    /// <summary>
    /// Deletes every row signed in with this Firebase uid, then the Firebase login. With no rows left it
    /// still deletes the login, which finishes a deletion that stopped after the database commit.
    /// </summary>
    Task<ResponseStatus> PurgeExternalIdAsync(string externalId, string requestedBy, bool refusePendingOrders = false);

    /// <summary>
    /// The accounts soft-deleted before deletion became permanent. A dry run reports counts and internal
    /// ids only; otherwise up to <paramref name="max"/> accounts are purged in id order, stopping at the
    /// first failure.
    /// </summary>
    Task<ResponseBase<string>> PurgeSoftDeletedAsync(bool dryRun, int max, string requestedBy);
}
