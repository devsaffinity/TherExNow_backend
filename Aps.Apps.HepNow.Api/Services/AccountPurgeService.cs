using System.Text.RegularExpressions;
using Aps.Apps.HepNow.Models.Dtos;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Stripe;

namespace Aps.Apps.HepNow.Services;

/// <summary>
/// Permanent account deletion, in three phases:
/// A) checks, then Stripe cancellation -- after the commit nothing links a charge to a person;
/// B) every row the account owns, in one transaction on one context;
/// C) after the commit, the systems outside the database: Firebase login, Stripe customer, S3 media,
///    webhook payloads, favourite counts. A database failure therefore never leaves someone locked out
///    of data that still exists, and a failure in C is logged for manual follow-up, never retried.
/// </summary>
public class AccountPurgeService : IAccountPurgeService
{
    private const string MediaKeyPrefix = "files/";
    private const int MaxBacklogBatch = 20;

    // Only objects the unique-key upload flow created ("files/<8 hex>-<name>") are deleted. Older keys
    // were plain file names that other accounts and catalogue rows could share.
    private static readonly Regex UniqueUploadName = new("^[0-9a-f]{8}-[^/]+$", RegexOptions.CultureInvariant);

    // Every foreign key that points at a row this service deletes. The backlog dry run compares the live
    // schema against it, because an unknown dependant would make every purge roll back.
    private static readonly HashSet<string> HandledForeignKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Notification.UserId->Users", "Notification.RequestId->Requests", "RequestProducts.RequestId->Requests",
        "RoutineExercises.RoutineId->Routines", "Requests.UserId->Users", "Routines.UserId->Users",
        "CustomizedExercise.UserId->Users", "FavoriteExercises.UserId->Users", "Memberships.UserId->Users",
        "ShoppingCart.UserId->Users", "UserActiveDay.UserId->Users", "PrintData.UserId->Users",
    };

    private readonly IDbContextFactory<DataContext> contextFactory;
    private readonly IFirebaseAccounts firebaseAccounts;
    private readonly IMediaStorage mediaStorage;
    private readonly IConfiguration configuration;
    private readonly ILogger<AccountPurgeService> logger;
    private readonly string stripeSecretKey;
    private IStripeClient stripeClient;

    public AccountPurgeService(IDbContextFactory<DataContext> contextFactory, IFirebaseAccounts firebaseAccounts, IMediaStorage mediaStorage,
        IOptions<StripeSettings> stripeSettings, IConfiguration configuration, ILogger<AccountPurgeService> logger, IStripeClient stripeClient = null)
    {
        this.contextFactory = contextFactory;
        this.firebaseAccounts = firebaseAccounts;
        this.mediaStorage = mediaStorage;
        this.configuration = configuration;
        this.logger = logger;
        this.stripeClient = stripeClient;
        stripeSecretKey = stripeSettings.Value?.Secretkey;
    }

    #region entry points
    public async Task<ResponseStatus> PurgeUserAsync(int userId, string requestedBy, bool refusePendingOrders = false, bool refuseAdminTarget = false)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();

        var requested = await ToAccountRows(AllUsers(ctx).Where(u => u.Id == userId)).FirstOrDefaultAsync();
        if (requested == null)
            return ResponseStatus.UserNotFound;

        if (refuseAdminTarget && requested.UserType == UserType.Admin)
            return ResponseStatus.NotAllowd;

        var uids = requested.Uids.ToList();
        var twins = uids.Count == 0
            ? new List<AccountRow>()
            : await ToAccountRows(AllUsers(ctx).Where(u => u.Id != userId && (uids.Contains(u.ExternalId) || uids.Contains(u.ExternalId2)))).ToListAsync();

        // Rows sharing a Firebase uid are the same login (Users has no unique index). Soft-deleted copies
        // always go with the account; live copies only when the account itself is live, so clearing a
        // leftover soft-deleted row never takes a working account, or an admin, with it.
        var accounts = twins
            .Where(t => t.IsDeleted || (!requested.IsDeleted && t.UserType != UserType.Admin))
            .Prepend(requested)
            .ToList();

        return await PurgeAsync(ctx, accounts, requestedBy, refusePendingOrders);
    }

    public async Task<ResponseStatus> PurgeExternalIdAsync(string externalId, string requestedBy, bool refusePendingOrders = false)
    {
        if (string.IsNullOrEmpty(externalId))
            return ResponseStatus.NotEnoghData;

        await using var ctx = await contextFactory.CreateDbContextAsync();

        var accounts = await ToAccountRows(AllUsers(ctx).Where(u => u.ExternalId == externalId || u.ExternalId2 == externalId)).ToListAsync();
        if (accounts.Count > 0)
            return await PurgeAsync(ctx, accounts, requestedBy, refusePendingOrders);

        // No rows: the account was never created, or a deletion committed and stopped before Firebase.
        // The caller holds a valid token for this uid, so finishing the job is theirs to ask for.
        logger.LogInformation("Account purge: no rows for Firebase uid {Uid}; deleting the login only (requested by {RequestedBy})", externalId, requestedBy);
        await ReleaseFirebaseUidsAsync(ctx, new[] { externalId });
        return ResponseStatus.Success;
    }

    public async Task<ResponseBase<string>> PurgeSoftDeletedAsync(bool dryRun, int max, string requestedBy)
    {
        await using var ctx = await contextFactory.CreateDbContextAsync();

        var softDeleted = await ToAccountRows(AllUsers(ctx).Where(u => u.IsDeleted).OrderBy(u => u.Id)).ToListAsync();

        if (dryRun)
            return ResponseBase<string>.Success(JsonConvert.SerializeObject(await DescribeBacklogAsync(ctx, softDeleted), Formatting.Indented));

        var batch = Math.Clamp(max, 1, MaxBacklogBatch);
        var processed = new List<object>();
        foreach (var account in softDeleted)
        {
            if (processed.Count >= batch)
                break;

            var status = await PurgeUserAsync(account.Id, requestedBy);

            // Already removed earlier in this batch as a twin of another row.
            if (status == ResponseStatus.UserNotFound)
                continue;

            processed.Add(new { userId = account.Id, code = status.Code, status = status.Value });
            if (status != ResponseStatus.Success)
                break;
        }

        var remaining = await AllUsers(ctx).CountAsync(u => u.IsDeleted);
        logger.LogInformation("Account purge backlog batch by {RequestedBy}: {Processed} processed, {Remaining} soft-deleted accounts remain", requestedBy, processed.Count, remaining);

        return ResponseBase<string>.Success(JsonConvert.SerializeObject(new { processed, remaining }, Formatting.Indented));
    }
    #endregion

    #region purge
    private async Task<ResponseStatus> PurgeAsync(DataContext ctx, List<AccountRow> accounts, string requestedBy, bool refusePendingOrders)
    {
        var userIds = accounts.Select(a => a.Id).ToList();

        // A3. Before anything changes. A soft-deleted order is not pending, so the filter stays on.
        if (refusePendingOrders && await ctx.Set<Requests>().AnyAsync(r => userIds.Contains(r.UserId) && r.RequestStatus == RequestStatus.Waiting))
            return CustomResponseStatus.PendingOrderExists;

        // A4. If billing cannot be stopped, nothing else happens; cancelling is idempotent, so a retry is safe.
        var stripe = await ReviewStripeAsync(ctx, accounts, cancel: true);
        if (!stripe.Succeeded)
        {
            logger.LogError("Account purge refused for users {UserIds} (requested by {RequestedBy}): Stripe subscriptions could not be cancelled ({Reason}); nothing was deleted",
                userIds, requestedBy, stripe.Error);
            return CustomResponseStatus.SubscriptionCancellationFailed;
        }

        if (stripe.Unverified.Count > 0)
            logger.LogWarning("Account purge for users {UserIds}: Stripe subscriptions {SubscriptionIds} carry these user ids in metadata but could not be tied to the account, so they were NOT cancelled; check them in the Stripe dashboard",
                userIds, stripe.Unverified);

        PurgedRows purged;
        try
        {
            purged = await DeleteRowsAsync(ctx, userIds);
        }
        catch (Exception ex)
        {
            // Disposing the uncommitted transaction rolled everything back; Firebase and S3 are untouched.
            logger.LogError(ex, "Account purge rolled back for users {UserIds} (requested by {RequestedBy}); no rows were deleted", userIds, requestedBy);
            return ResponseStatus.UnknownError;
        }

        var uids = accounts.SelectMany(a => a.Uids).Distinct().ToList();

        // C1. The only record left of the account. No email or name.
        logger.LogInformation("Account purge committed: users {UserIds} requested by {RequestedBy}; Firebase uids {Uids}; Stripe customers {CustomerIds}; cancelled subscriptions {SubscriptionIds}; {MediaKeyCount} media keys; {ActiveStoreSubscriptions} Apple/Google subscriptions still running; rows deleted {Rows}",
            userIds, requestedBy, uids, stripe.CustomerIds, stripe.Cancelled, purged.MediaKeys.Count, purged.ActiveStoreSubscriptions,
            string.Join(", ", purged.Rows.Select(r => $"{r.Key}={r.Value}")));

        await ReleaseFirebaseUidsAsync(ctx, uids);
        await DeleteStripeCustomersAsync(ctx, stripe.CustomerIds);
        await DeleteMediaAsync(ctx, purged.MediaKeys, purged.LegacyMediaReferences);
        await AnonymiseWebhookLogsAsync(ctx, stripe.CustomerIds.Concat(purged.PurchaseTokens));
        await RecountFavouritesAsync(ctx, purged.FavouriteExerciseIds);

        return ResponseStatus.Success;
    }

    private async Task<PurgedRows> DeleteRowsAsync(DataContext ctx, List<int> userIds)
    {
        var now = DateTime.UtcNow;
        await using var tx = await ctx.Database.BeginTransactionAsync();

        // B1. Lock the parents first. Inserting a child row takes a shared lock on its parent, so a sign-in
        // day, webhook membership or new order arriving now waits for this transaction and then fails its
        // foreign key check, instead of making the Users delete below fail.
        await AllUsers(ctx).Where(u => userIds.Contains(u.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsDeleted, true).SetProperty(u => u.LastModifiedDate, now));

        var requestIds = await All<Requests>(ctx).Where(r => userIds.Contains(r.UserId)).Select(r => r.Id).ToListAsync();
        if (requestIds.Count > 0)
            await All<Requests>(ctx).Where(r => requestIds.Contains(r.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsDeleted, true).SetProperty(r => r.LastModifiedDate, now));

        var routineIds = await All<Routines>(ctx).Where(r => userIds.Contains(r.UserId)).Select(r => r.Id).ToListAsync();

        // A2, read under the locks so nothing can be added between reading and deleting.
        var mediaReferences = (await AllUsers(ctx).Where(u => userIds.Contains(u.Id)).Select(u => u.PhotoUrl).ToListAsync())
            .Concat(await All<Routines>(ctx).Where(r => userIds.Contains(r.UserId)).Select(r => r.FileUrl).ToListAsync())
            .Concat(await All<CustomizedExercise>(ctx).Where(c => userIds.Contains(c.UserId)).Select(c => c.OtherMediaUrl).ToListAsync())
            .ToList();
        var memberships = await All<Memberships>(ctx).Where(m => userIds.Contains(m.UserId))
            .Select(m => new { m.Provider, m.EndDate, m.PurchaseToken }).ToListAsync();

        var purged = new PurgedRows
        {
            MediaKeys = MediaKeysIn(mediaReferences, configuration["S3Credentials:BucketName"], out var legacyReferences),
            LegacyMediaReferences = legacyReferences,
            FavouriteExerciseIds = await All<FavoriteExercises>(ctx).Where(f => userIds.Contains(f.UserId)).Select(f => f.ExerciseId).Distinct().ToListAsync(),
            PurchaseTokens = memberships.Select(m => m.PurchaseToken).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList(),
            ActiveStoreSubscriptions = memberships.Count(m => (m.Provider == MembershipProvider.Apple || m.Provider == MembershipProvider.Google) && m.EndDate > now),
        };

        // B2-B14, children before parents. IgnoreQueryFilters throughout: soft-deleted children still hold
        // RESTRICT foreign keys, and normal use creates them all the time (cleared carts, un-favourites).
        var rows = purged.Rows;

        // Includes the admins' fan-out notifications that point at this member's orders.
        rows["Notification"] = await All<Notification>(ctx)
            .Where(n => userIds.Contains(n.UserId) || (n.RequestId != null && requestIds.Contains(n.RequestId.Value)))
            .ExecuteDeleteAsync();
        rows["RequestProducts"] = requestIds.Count == 0 ? 0 : await All<RequestProducts>(ctx).Where(p => requestIds.Contains(p.RequestId)).ExecuteDeleteAsync();
        rows["RoutineExercises"] = routineIds.Count == 0 ? 0 : await All<RoutineExercises>(ctx).Where(e => routineIds.Contains(e.RoutineId)).ExecuteDeleteAsync();
        rows["Requests"] = await All<Requests>(ctx).Where(r => userIds.Contains(r.UserId)).ExecuteDeleteAsync();
        rows["Routines"] = await All<Routines>(ctx).Where(r => userIds.Contains(r.UserId)).ExecuteDeleteAsync();
        rows["CustomizedExercise"] = await All<CustomizedExercise>(ctx).Where(c => userIds.Contains(c.UserId)).ExecuteDeleteAsync();
        rows["FavoriteExercises"] = await All<FavoriteExercises>(ctx).Where(f => userIds.Contains(f.UserId)).ExecuteDeleteAsync();
        rows["Memberships"] = await All<Memberships>(ctx).Where(m => userIds.Contains(m.UserId)).ExecuteDeleteAsync();
        rows["ShoppingCart"] = await All<ShoppingCart>(ctx).Where(c => userIds.Contains(c.UserId)).ExecuteDeleteAsync();
        rows["UserActiveDay"] = await All<UserActiveDay>(ctx).Where(d => userIds.Contains(d.UserId)).ExecuteDeleteAsync();
        rows["PrintData"] = await All<PrintData>(ctx).Where(p => userIds.Contains(p.UserId)).ExecuteDeleteAsync();
        rows["Users"] = await AllUsers(ctx).Where(u => userIds.Contains(u.Id)).ExecuteDeleteAsync();

        // Thrown rather than returned so the transaction is disposed uncommitted and rolls back.
        if (rows["Users"] != userIds.Count)
            throw new InvalidOperationException($"Expected to delete {userIds.Count} Users rows but deleted {rows["Users"]}.");

        await tx.CommitAsync();
        return purged;
    }
    #endregion

    #region stripe
    private async Task<StripeReview> ReviewStripeAsync(DataContext ctx, List<AccountRow> accounts, bool cancel)
    {
        var review = new StripeReview();
        var client = StripeClient();
        if (client == null)
        {
            review.Error = "Stripe is not configured";
            return review;
        }

        // dev inherits the bundle's StripeSettings; a live key there must never cancel real subscriptions.
        if (configuration["env"] != "prd" && (client.ApiKey?.StartsWith("sk_live_") == true || client.ApiKey?.StartsWith("rk_live_") == true))
        {
            review.Error = "a live Stripe key outside prd";
            return review;
        }

        var userIds = accounts.Select(a => a.Id).ToList();
        var emails = accounts.Select(a => a.Email).Where(e => !string.IsNullOrEmpty(e)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var storedCustomerIds = accounts.Select(a => a.StripeCustomerId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();

        // A customer another account still uses is not this account's to cancel or delete.
        var sharedCustomerIds = storedCustomerIds.Count == 0
            ? new List<string>()
            : await AllUsers(ctx).Where(u => !userIds.Contains(u.Id) && storedCustomerIds.Contains(u.StripeCustomerId)).Select(u => u.StripeCustomerId).Distinct().ToListAsync();

        var subscriptions = new Dictionary<string, Subscription>();
        var service = new SubscriptionService(client);
        try
        {
            foreach (var customerId in storedCustomerIds.Except(sharedCustomerIds))
            {
                review.CustomerIds.Add(customerId);
                await foreach (var subscription in service.ListAutoPagingAsync(new SubscriptionListOptions { Customer = customerId, Status = "all" }))
                    subscriptions[subscription.Id] = subscription;
            }

            // A first checkout passes Customer = null (CreateSubscription reads StripeCustomerId from a copy
            // CreateCustomer never updates), so Checkout creates a second customer that no Users row points
            // at. Those subscriptions are reachable only through the userId metadata this backend sets.
            foreach (var userId in userIds)
            {
                var search = new SubscriptionSearchOptions { Query = $"metadata['userId']:'{userId}'", Expand = new List<string> { "data.customer" } };
                await foreach (var subscription in service.SearchAutoPagingAsync(search))
                {
                    if (subscriptions.ContainsKey(subscription.Id))
                        continue;

                    // Metadata alone is not proof -- ids from an earlier database could collide -- so the
                    // customer must carry this account's email and belong to no remaining account.
                    var tied = subscription.Metadata?.ContainsKey("planId") == true
                        && subscription.Customer?.Email != null
                        && emails.Contains(subscription.Customer.Email)
                        && !await AllUsers(ctx).AnyAsync(u => !userIds.Contains(u.Id) && u.StripeCustomerId == subscription.CustomerId);

                    if (!tied)
                    {
                        review.Unverified.Add(subscription.Id);
                        continue;
                    }

                    subscriptions[subscription.Id] = subscription;
                    if (!review.CustomerIds.Contains(subscription.CustomerId))
                        review.CustomerIds.Add(subscription.CustomerId);
                }
            }

            foreach (var subscription in subscriptions.Values)
            {
                if (subscription.Status is "canceled" or "incomplete_expired")
                    continue;

                review.Open.Add(subscription.Id);
                if (cancel)
                {
                    await service.CancelAsync(subscription.Id);
                    review.Cancelled.Add(subscription.Id);
                }
            }

            review.Succeeded = true;
        }
        catch (StripeException ex)
        {
            review.Error = ex.StripeError?.Code ?? ex.GetType().Name;
            logger.LogError(ex, "Account purge: Stripe review failed for users {UserIds}", userIds);
        }

        return review;
    }

    private async Task DeleteStripeCustomersAsync(DataContext ctx, IEnumerable<string> customerIds)
    {
        foreach (var customerId in customerIds.Distinct().ToList())
        {
            try
            {
                if (await AllUsers(ctx).AnyAsync(u => u.StripeCustomerId == customerId))
                {
                    logger.LogInformation("Account purge: kept Stripe customer {CustomerId}, another account uses it", customerId);
                    continue;
                }

                await new CustomerService(StripeClient()).DeleteAsync(customerId);
            }
            catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
            {
                // Already deleted.
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Account purge: Stripe customer {CustomerId} was not deleted; delete it in the Stripe dashboard", customerId);
            }
        }
    }

    private IStripeClient StripeClient() =>
        stripeClient ??= string.IsNullOrWhiteSpace(stripeSecretKey) ? null : new StripeClient(stripeSecretKey);
    #endregion

    #region after commit
    private async Task ReleaseFirebaseUidsAsync(DataContext ctx, IEnumerable<string> uids)
    {
        foreach (var uid in uids.Distinct().ToList())
        {
            try
            {
                // A live account elsewhere may still sign in with this uid (see the twin rule above).
                if (await AllUsers(ctx).AnyAsync(u => u.ExternalId == uid || u.ExternalId2 == uid))
                {
                    logger.LogInformation("Account purge: kept Firebase uid {Uid}, another account row uses it", uid);
                    continue;
                }

                await firebaseAccounts.DeleteAsync(uid);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Account purge: Firebase uid {Uid} was not deleted; delete it in the Firebase console", uid);
            }
        }
    }

    private async Task DeleteMediaAsync(DataContext ctx, IReadOnlyList<string> keys, int legacyReferences)
    {
        if (legacyReferences > 0)
            logger.LogInformation("Account purge: {Count} media references were not in the unique upload format and were left in S3", legacyReferences);

        if (keys.Count == 0)
            return;

        if (!mediaStorage.CanDelete)
        {
            logger.LogInformation("Account purge: S3 skipped outside prd ({Count} keys)", keys.Count);
            return;
        }

        foreach (var key in keys)
        {
            try
            {
                var name = key.Substring(MediaKeyPrefix.Length);
                if (await IsMediaReferencedAsync(ctx, name))
                {
                    logger.LogInformation("Account purge: kept S3 key {Key}, a remaining row references it", key);
                    continue;
                }

                await mediaStorage.DeleteAllVersionsAsync(key);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Account purge: S3 key {Key} was not deleted; remove every version by hand", key);
            }
        }
    }

    private static async Task<bool> IsMediaReferencedAsync(DataContext ctx, string name) =>
        await AllUsers(ctx).AnyAsync(u => u.PhotoUrl.Contains(name))
        || await All<Exercises>(ctx).AnyAsync(e => e.PhotoUrl.Contains(name) || e.OtherMediaUrl.Contains(name))
        || await All<Products>(ctx).AnyAsync(p => p.PhotoUrl.Contains(name) || p.OtherMediaUrl.Contains(name))
        || await All<CustomizedExercise>(ctx).AnyAsync(c => c.OtherMediaUrl.Contains(name))
        || await All<Routines>(ctx).AnyAsync(r => r.FileUrl.Contains(name))
        || await All<PrintData>(ctx).AnyAsync(p => p.Data.Contains(name));

    private async Task AnonymiseWebhookLogsAsync(DataContext ctx, IEnumerable<string> identifiers)
    {
        foreach (var identifier in identifiers.Distinct().ToList())
        {
            try
            {
                // Quoted, as the value appears in the JSON payload, so one id cannot match inside a longer one.
                // EventId stays: it is Stripe's redelivery key. Apple's signed payloads cannot be matched.
                var quoted = $"\"{identifier}\"";
                await All<WebhookLog>(ctx).Where(w => w.Data != null && w.Data.Contains(quoted))
                    .ExecuteUpdateAsync(s => s.SetProperty(w => w.Data, (string)null));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Account purge: webhook payloads containing {Identifier} were not cleared", identifier);
            }
        }
    }

    private async Task RecountFavouritesAsync(DataContext ctx, List<int> exerciseIds)
    {
        if (exerciseIds.Count == 0)
            return;

        try
        {
            // Same rule as FavoriteExerciseService.UpdateFavoriteCount: soft-deleted favourites do not count. Spelled
            // out, because the global IsDeleted filter is not applied to a subquery inside an ExecuteUpdate setter.
            await All<Exercises>(ctx).Where(e => exerciseIds.Contains(e.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.FavoriteCount, e => All<FavoriteExercises>(ctx).Count(f => f.ExerciseId == e.Id && !f.IsDeleted)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Account purge: favourite counts for exercises {ExerciseIds} were not recomputed", exerciseIds);
        }
    }
    #endregion

    #region backlog report
    private async Task<object> DescribeBacklogAsync(DataContext ctx, List<AccountRow> softDeleted)
    {
        var userIds = softDeleted.Select(a => a.Id).ToList();
        var now = DateTime.UtcNow;

        var liveUids = await AllUsers(ctx).Where(u => !u.IsDeleted).Select(u => new { u.ExternalId, u.ExternalId2 }).ToListAsync();
        var liveUidSet = liveUids.SelectMany(u => new[] { u.ExternalId, u.ExternalId2 }).Where(uid => !string.IsNullOrEmpty(uid)).ToHashSet();

        var requestIds = await All<Requests>(ctx).Where(r => userIds.Contains(r.UserId)).Select(r => r.Id).ToListAsync();
        var routineIds = await All<Routines>(ctx).Where(r => userIds.Contains(r.UserId)).Select(r => r.Id).ToListAsync();

        var memberships = await All<Memberships>(ctx).Where(m => userIds.Contains(m.UserId) && m.EndDate > now
                && (m.Provider == MembershipProvider.Apple || m.Provider == MembershipProvider.Google))
            .Select(m => m.Provider).ToListAsync();

        var mediaReferences = softDeleted.Select(a => a.PhotoUrl)
            .Concat(await All<Routines>(ctx).Where(r => userIds.Contains(r.UserId)).Select(r => r.FileUrl).ToListAsync())
            .Concat(await All<CustomizedExercise>(ctx).Where(c => userIds.Contains(c.UserId)).Select(c => c.OtherMediaUrl).ToListAsync());
        var mediaKeys = MediaKeysIn(mediaReferences, configuration["S3Credentials:BucketName"], out var legacyReferences);

        // One account at a time, a few in parallel: the report must finish inside the load balancer's timeout.
        var stripeReviews = new List<StripeReview>();
        using (var gate = new SemaphoreSlim(4))
        {
            var tasks = softDeleted.Select(async account =>
            {
                await gate.WaitAsync();
                try
                {
                    await using var accountCtx = await contextFactory.CreateDbContextAsync();
                    return await ReviewStripeAsync(accountCtx, new List<AccountRow> { account }, cancel: false);
                }
                finally
                {
                    gate.Release();
                }
            });
            stripeReviews.AddRange(await Task.WhenAll(tasks));
        }

        return new
        {
            softDeletedAccounts = softDeleted.Count,
            userIds,
            withStripeCustomerId = softDeleted.Count(a => !string.IsNullOrEmpty(a.StripeCustomerId)),
            stripe = new
            {
                accountsWithOpenSubscriptions = stripeReviews.Count(r => r.Open.Count > 0),
                openSubscriptions = stripeReviews.Sum(r => r.Open.Count),
                metadataMatchesThatWouldNotBeCancelled = stripeReviews.Sum(r => r.Unverified.Count),
                errors = stripeReviews.Where(r => !r.Succeeded).Select(r => r.Error).Distinct().ToList(),
            },
            appleSubscriptionsStillRunning = memberships.Count(p => p == MembershipProvider.Apple),
            googleSubscriptionsStillRunning = memberships.Count(p => p == MembershipProvider.Google),
            accountsWithWaitingOrders = await ctx.Set<Requests>().Where(r => userIds.Contains(r.UserId) && r.RequestStatus == RequestStatus.Waiting)
                .Select(r => r.UserId).Distinct().CountAsync(),
            accountsSharingUidWithLiveAccount = softDeleted.Count(a => a.Uids.Any(liveUidSet.Contains)),
            childRows = new Dictionary<string, int>
            {
                ["Notification"] = await All<Notification>(ctx).CountAsync(n => userIds.Contains(n.UserId)),
                ["NotificationOfOthersOnTheirOrders"] = await All<Notification>(ctx).CountAsync(n => !userIds.Contains(n.UserId) && n.RequestId != null && requestIds.Contains(n.RequestId.Value)),
                ["RequestProducts"] = await All<RequestProducts>(ctx).CountAsync(p => requestIds.Contains(p.RequestId)),
                ["RoutineExercises"] = await All<RoutineExercises>(ctx).CountAsync(e => routineIds.Contains(e.RoutineId)),
                ["Requests"] = requestIds.Count,
                ["Routines"] = routineIds.Count,
                ["CustomizedExercise"] = await All<CustomizedExercise>(ctx).CountAsync(c => userIds.Contains(c.UserId)),
                ["FavoriteExercises"] = await All<FavoriteExercises>(ctx).CountAsync(f => userIds.Contains(f.UserId)),
                ["Memberships"] = await All<Memberships>(ctx).CountAsync(m => userIds.Contains(m.UserId)),
                ["ShoppingCart"] = await All<ShoppingCart>(ctx).CountAsync(c => userIds.Contains(c.UserId)),
                ["UserActiveDay"] = await All<UserActiveDay>(ctx).CountAsync(d => userIds.Contains(d.UserId)),
                ["PrintData"] = await All<PrintData>(ctx).CountAsync(p => userIds.Contains(p.UserId)),
            },
            media = new { deletableKeyCandidates = mediaKeys.Count, legacyReferencesLeftInS3 = legacyReferences, s3DeletesEnabled = mediaStorage.CanDelete },
            foreignKeys = await DescribeForeignKeysAsync(ctx),
        };
    }

    private static async Task<object> DescribeForeignKeysAsync(DataContext ctx)
    {
        if (!ctx.Database.IsRelational() || ctx.Database.ProviderName?.Contains("MySql", StringComparison.OrdinalIgnoreCase) != true)
            return "unavailable on this database provider";

        var keys = await ctx.Database.SqlQueryRaw<ForeignKeyRow>(@"
            SELECT kcu.TABLE_NAME AS ChildTable, kcu.COLUMN_NAME AS ChildColumn, kcu.REFERENCED_TABLE_NAME AS ParentTable, rc.DELETE_RULE AS DeleteRule
            FROM information_schema.KEY_COLUMN_USAGE kcu
            JOIN information_schema.REFERENTIAL_CONSTRAINTS rc
              ON rc.CONSTRAINT_SCHEMA = kcu.CONSTRAINT_SCHEMA AND rc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
            WHERE kcu.CONSTRAINT_SCHEMA = DATABASE() AND kcu.REFERENCED_TABLE_NAME IS NOT NULL").ToListAsync();

        var onPurgedTables = keys.Where(k => k.ParentTable is not null
                && new[] { "Users", "Requests", "Routines" }.Contains(k.ParentTable, StringComparer.OrdinalIgnoreCase))
            .Select(k => $"{k.ChildTable}.{k.ChildColumn}->{k.ParentTable}")
            .ToList();

        return new
        {
            total = keys.Count,
            nonRestrict = keys.Where(k => !string.Equals(k.DeleteRule, "RESTRICT", StringComparison.OrdinalIgnoreCase) && !string.Equals(k.DeleteRule, "NO ACTION", StringComparison.OrdinalIgnoreCase))
                .Select(k => $"{k.ChildTable}.{k.ChildColumn}->{k.ParentTable} {k.DeleteRule}").ToList(),
            dependantsThePurgeDoesNotDelete =onPurgedTables.Where(k => !HandledForeignKeys.Contains(k)).ToList(),
        };
    }
    #endregion

    #region media keys
    /// <summary>
    /// The S3 keys under <c>files/</c> that stored media references point at, limited to names the
    /// unique-key upload flow produces. <paramref name="legacyReferences"/> counts references left alone.
    /// </summary>
    public static IReadOnlyList<string> MediaKeysIn(IEnumerable<string> storedValues, string bucketName, out int legacyReferences)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        legacyReferences = 0;

        foreach (var reference in storedValues.SelectMany(SplitMediaList))
        {
            var key = ToMediaKey(reference, bucketName);
            if (key != null && UniqueUploadName.IsMatch(key.Substring(MediaKeyPrefix.Length)))
                keys.Add(key);
            else
                legacyReferences++;
        }

        return keys.ToList();
    }

    // OtherMediaUrl holds a JSON list (single-quoted in older rows); the other columns hold one value.
    private static IEnumerable<string> SplitMediaList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Array.Empty<string>();

        var trimmed = value.Trim();
        if (!trimmed.StartsWith('['))
            return new[] { trimmed };

        try
        {
            return JArray.Parse(trimmed).Where(item => item.Type == JTokenType.String).Select(item => item.Value<string>()).ToList();
        }
        catch (JsonException)
        {
            return new[] { trimmed };
        }
    }

    // Stored values are a bare upload name (web, admin), "files/<name>", or this bucket's URL. Anything
    // else -- another host, a nested path -- cannot be mapped to a key with certainty and is skipped.
    private static string ToMediaKey(string reference, string bucketName)
    {
        var value = reference?.Trim();
        if (string.IsNullOrEmpty(value))
            return null;

        string key;
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrEmpty(bucketName) || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || !uri.Host.StartsWith(bucketName + ".s3.", StringComparison.OrdinalIgnoreCase)
                || !uri.Host.EndsWith(".amazonaws.com", StringComparison.OrdinalIgnoreCase))
                return null;

            key = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        }
        else if (value.StartsWith(MediaKeyPrefix, StringComparison.Ordinal))
        {
            key = value;
        }
        else if (!value.Contains('/'))
        {
            key = MediaKeyPrefix + value;
        }
        else
        {
            return null;
        }

        var name = key.StartsWith(MediaKeyPrefix, StringComparison.Ordinal) ? key.Substring(MediaKeyPrefix.Length) : null;
        return string.IsNullOrEmpty(name) || name.Contains('/') ? null : key;
    }
    #endregion

    #region helpers
    private static IQueryable<Users> AllUsers(DataContext ctx) => ctx.Set<Users>().IgnoreQueryFilters();

    private static IQueryable<T> All<T>(DataContext ctx) where T : EntityDef => ctx.Set<T>().IgnoreQueryFilters();

    private static IQueryable<AccountRow> ToAccountRows(IQueryable<Users> users) =>
        users.Select(u => new AccountRow(u.Id, u.ExternalId, u.ExternalId2, u.Email, u.StripeCustomerId, u.PhotoUrl, u.UserType, u.IsDeleted));

    private sealed record AccountRow(int Id, string ExternalId, string ExternalId2, string Email, string StripeCustomerId, string PhotoUrl, UserType UserType, bool IsDeleted)
    {
        public IEnumerable<string> Uids => new[] { ExternalId, ExternalId2 }.Where(uid => !string.IsNullOrEmpty(uid));
    }

    private sealed class PurgedRows
    {
        public Dictionary<string, int> Rows { get; } = new();
        public IReadOnlyList<string> MediaKeys { get; init; }
        public int LegacyMediaReferences { get; init; }
        public List<int> FavouriteExerciseIds { get; init; }
        public List<string> PurchaseTokens { get; init; }
        public int ActiveStoreSubscriptions { get; init; }
    }

    private sealed class StripeReview
    {
        public bool Succeeded { get; set; }
        public string Error { get; set; }
        public List<string> CustomerIds { get; } = new();
        public List<string> Open { get; } = new();
        public List<string> Cancelled { get; } = new();
        public List<string> Unverified { get; } = new();
    }

    private sealed class ForeignKeyRow
    {
        public string ChildTable { get; set; }
        public string ChildColumn { get; set; }
        public string ParentTable { get; set; }
        public string DeleteRule { get; set; }
    }
    #endregion
}
