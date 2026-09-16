using System.Security.Claims;
using Aps.Apps.HepNow.Generics.Responses;
using Aps.Apps.HepNow.GraphQL.Mutations;
using Aps.Apps.HepNow.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aps.Apps.HepNow.Test.ServicesTest
{
    public class AccountPurgeTest
    {
        [Fact]
        public async Task Purge_RemovesEveryRowTheAccountOwns_AndLeavesOtherAccountsAlone()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var admin = await h.SeedAdminAsync("admin-uid");
            var member = await h.SeedMemberAsync(catalogue, "member-uid", new MemberOptions { NotifyAdminId = admin, PhotoUrl = "0a1b2c3d-me.jpg" });
            var other = await h.SeedMemberAsync(catalogue, "other-uid", new MemberOptions { NotifyAdminId = admin });
            var otherBefore = await h.RowsOwnedByAsync(other);

            var status = await h.Purge.PurgeUserAsync(member, "self:test", refusePendingOrders: true);

            Assert.Equal(ResponseStatus.Success, status);
            foreach (var (table, count) in await h.RowsOwnedByAsync(member))
                Assert.True(count == 0, $"{table} still has {count} rows for the purged account");

            Assert.Equal(otherBefore, await h.RowsOwnedByAsync(other));
            Assert.Equal(1, await h.CountAsync<Notification>(n => n.UserId == admin));
            Assert.NotNull(await h.FindAsync<Users>(admin));
            Assert.NotNull(await h.FindAsync<Exercises>(catalogue.ExerciseId));
            Assert.NotNull(await h.FindAsync<Products>(catalogue.ProductId));
            Assert.NotNull(await h.FindAsync<Plans>(catalogue.PlanId));

            Assert.Equal(new[] { "member-uid" }, h.Firebase.Deleted);
            Assert.Equal(new[] { "files/0a1b2c3d-me.jpg" }, h.Media.Deleted);

            // Only the other member's live favourite still counts.
            Assert.Equal(1, (await h.FindAsync<Exercises>(catalogue.ExerciseId))!.FavoriteCount);
        }

        [Fact]
        public async Task SelfPurge_WithAWaitingOrder_IsRefusedBeforeAnythingChanges()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "waiting-uid", new MemberOptions { WaitingOrder = true, StripeCustomerId = "cus_waiting" });
            h.Stripe.Subscriptions.Add(new FakeSubscription { Id = "sub_waiting", CustomerId = "cus_waiting" });
            var before = await h.RowsOwnedByAsync(member);

            var status = await h.Purge.PurgeUserAsync(member, "self:test", refusePendingOrders: true);

            Assert.Equal(CustomResponseStatus.PendingOrderExists, status);
            Assert.Equal(before, await h.RowsOwnedByAsync(member));
            Assert.Empty(h.Stripe.Requests);
            Assert.Empty(h.Firebase.Deleted);
        }

        [Fact]
        public async Task AdminPurge_GoesAheadDespiteAWaitingOrder_ButNeverDeletesAnAdmin()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var admin = await h.SeedAdminAsync("admin-uid");
            var otherAdmin = await h.SeedAdminAsync("other-admin-uid");
            var member = await h.SeedMemberAsync(catalogue, "ordering-uid", new MemberOptions { WaitingOrder = true });

            Assert.Equal(ResponseStatus.NotAllowd, await h.Purge.PurgeUserAsync(otherAdmin, $"admin:{admin}", refuseAdminTarget: true));
            Assert.NotNull(await h.FindAsync<Users>(otherAdmin));

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeUserAsync(member, $"admin:{admin}", refuseAdminTarget: true));
            Assert.Null(await h.FindAsync<Users>(member));
        }

        [Fact]
        public async Task WhenStripeFails_NothingIsDeleted()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "billing-uid", new MemberOptions { StripeCustomerId = "cus_billing" });
            var before = await h.RowsOwnedByAsync(member);
            h.Stripe.Fail = true;

            var status = await h.Purge.PurgeUserAsync(member, "self:test");

            Assert.Equal(CustomResponseStatus.SubscriptionCancellationFailed, status);
            Assert.Equal(before, await h.RowsOwnedByAsync(member));
            Assert.Empty(h.Firebase.Deleted);
            Assert.Empty(h.Media.Deleted);
        }

        [Fact]
        public async Task ALiveStripeKeyOutsideProduction_IsNeverUsed()
        {
            await using var h = new PurgeHarness(env: "dev", stripeKey: "sk_live_unit");
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "dev-uid", new MemberOptions { StripeCustomerId = "cus_dev" });

            var status = await h.Purge.PurgeUserAsync(member, "self:test");

            Assert.Equal(CustomResponseStatus.SubscriptionCancellationFailed, status);
            Assert.Empty(h.Stripe.Requests);
            Assert.NotNull(await h.FindAsync<Users>(member));
        }

        [Fact]
        public async Task Stripe_CancelsEveryOpenSubscriptionTiedToTheAccount_AndDeletesItsCustomers()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "subscriber-uid", new MemberOptions { Email = "subscriber@example.com", StripeCustomerId = "cus_stored" });
            var metadata = new Dictionary<string, string> { ["userId"] = member.ToString(), ["planId"] = "2" };

            h.Stripe.Subscriptions.AddRange(new[]
            {
                new FakeSubscription { Id = "sub_active", CustomerId = "cus_stored" },
                new FakeSubscription { Id = "sub_trialing", CustomerId = "cus_stored", Status = "trialing" },
                new FakeSubscription { Id = "sub_past_due", CustomerId = "cus_stored", Status = "past_due" },
                new FakeSubscription { Id = "sub_unpaid", CustomerId = "cus_stored", Status = "unpaid" },
                new FakeSubscription { Id = "sub_already_cancelled", CustomerId = "cus_stored", Status = "canceled" },
                // The second customer a first checkout creates: found through metadata, same email.
                new FakeSubscription { Id = "sub_second_customer", CustomerId = "cus_second", CustomerEmail = "SUBSCRIBER@example.com", Metadata = metadata },
                // Same metadata, someone else's email: never cancelled on metadata alone.
                new FakeSubscription { Id = "sub_stranger", CustomerId = "cus_stranger", CustomerEmail = "stranger@example.com", Metadata = metadata },
            });

            var status = await h.Purge.PurgeUserAsync(member, "self:test");

            Assert.Equal(ResponseStatus.Success, status);
            Assert.Equal(new[] { "sub_active", "sub_past_due", "sub_second_customer", "sub_trialing", "sub_unpaid" }, h.Stripe.Cancelled.OrderBy(id => id));
            Assert.Equal(new[] { "cus_second", "cus_stored" }, h.Stripe.DeletedCustomers.OrderBy(id => id));
        }

        [Fact]
        public async Task AStripeCustomerAnotherAccountStillUses_IsNotCancelledOrDeleted()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            await h.SeedMemberAsync(catalogue, "live-uid", new MemberOptions { StripeCustomerId = "cus_shared" });
            var leftover = await h.SeedMemberAsync(catalogue, "leftover-uid", new MemberOptions { StripeCustomerId = "cus_shared", SoftDeleted = true });
            h.Stripe.Subscriptions.Add(new FakeSubscription { Id = "sub_shared", CustomerId = "cus_shared" });

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeUserAsync(leftover, "backlog:test"));

            Assert.Empty(h.Stripe.Cancelled);
            Assert.Empty(h.Stripe.DeletedCustomers);
        }

        [Fact]
        public async Task AnUnknownDependentRow_RollsTheWholeDeletionBack()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "rollback-uid", new MemberOptions { PhotoUrl = "3c4d5e6f-rollback.jpg" });
            var before = await h.RowsOwnedByAsync(member);

            // A table this code does not know about, holding a RESTRICT key to the account.
            await h.ExecuteSqlAsync(h.IsMySql
                ? "CREATE TABLE Unrelated (Id INT AUTO_INCREMENT PRIMARY KEY, UserId INT NOT NULL, CONSTRAINT FK_Unrelated_Users FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE RESTRICT)"
                : "CREATE TABLE \"Unrelated\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"UserId\" INTEGER NOT NULL REFERENCES \"Users\" (\"Id\") ON DELETE RESTRICT)");
            await h.ExecuteSqlAsync($"INSERT INTO Unrelated (UserId) VALUES ({member})");

            var status = await h.Purge.PurgeUserAsync(member, "self:test");

            Assert.Equal(ResponseStatus.UnknownError, status);
            Assert.Equal(before, await h.RowsOwnedByAsync(member));
            Assert.False((await h.FindAsync<Users>(member))!.IsDeleted);
            Assert.Empty(h.Firebase.Deleted);
            Assert.Empty(h.Media.Deleted);
        }

        [Fact]
        public async Task PurgingALiveAccount_TakesItsSoftDeletedCopies()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var live = await h.SeedMemberAsync(catalogue, "twin-uid");
            var copy = await h.SeedMemberAsync(catalogue, "twin-uid", new MemberOptions { SoftDeleted = true });

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeUserAsync(live, "self:test"));

            Assert.Null(await h.FindAsync<Users>(live));
            Assert.Null(await h.FindAsync<Users>(copy));
            Assert.Equal(new[] { "twin-uid" }, h.Firebase.Deleted);
        }

        [Fact]
        public async Task PurgingASoftDeletedCopy_LeavesTheLiveAccountAndItsLogin()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var live = await h.SeedMemberAsync(catalogue, "shared-uid");
            var copy = await h.SeedMemberAsync(catalogue, "shared-uid", new MemberOptions { SoftDeleted = true });

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeUserAsync(copy, "backlog:test"));

            Assert.Null(await h.FindAsync<Users>(copy));
            Assert.NotNull(await h.FindAsync<Users>(live));
            Assert.Empty(h.Firebase.Deleted);
        }

        [Fact]
        public async Task PurgeByUid_WithNoRowsLeft_DeletesTheLogin()
        {
            await using var h = new PurgeHarness();

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeExternalIdAsync("ghost-uid", "self:uid", refusePendingOrders: true));

            Assert.Equal(new[] { "ghost-uid" }, h.Firebase.Deleted);
            Assert.Empty(h.Stripe.Requests);
        }

        [Fact]
        public async Task AFirebaseFailureAfterTheCommit_StillReportsSuccess()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "primary-uid", new MemberOptions { LinkedUid = "linked-uid" });
            h.Firebase.FailOn.Add("primary-uid");

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeUserAsync(member, "self:test"));

            Assert.Null(await h.FindAsync<Users>(member));
            Assert.Equal(new[] { "linked-uid" }, h.Firebase.Deleted);
        }

        [Fact]
        public async Task Media_OnlyUniqueUploadsNoRemainingRowUses_AreDeleted_AndOnlyInProduction()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "media-uid", new MemberOptions
            {
                PhotoUrl = "0a1b2c3d-photo.jpg",
                RoutineFileUrl = "https://therexnow-bucket.s3.us-west-1.amazonaws.com/files/1a2b3c4d-plan.pdf",
                // A copy of catalogue media, a pre-uuid name, and a legacy Azure URL: none may be deleted.
                CustomizedMedia = "['a1b2c3d4-shared-demo.jpg', 'knee-video.mp4', 'https://apshepnowstorage.blob.core.windows.net/images/2b3c4d5e-x.jpg']",
            });

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeUserAsync(member, "self:test"));
            Assert.Equal(new[] { "files/0a1b2c3d-photo.jpg", "files/1a2b3c4d-plan.pdf" }, h.Media.Deleted.OrderBy(k => k));

            await using var dev = new PurgeHarness(env: "dev");
            dev.Media.CanDelete = false;
            var devCatalogue = await dev.SeedCatalogueAsync();
            var devMember = await dev.SeedMemberAsync(devCatalogue, "dev-media-uid", new MemberOptions { PhotoUrl = "4d5e6f70-photo.jpg" });

            Assert.Equal(ResponseStatus.Success, await dev.Purge.PurgeUserAsync(devMember, "self:test"));
            Assert.Empty(dev.Media.Deleted);
        }

        [Theory]
        [InlineData("0a1b2c3d-photo.jpg", "files/0a1b2c3d-photo.jpg")]
        [InlineData("files/0a1b2c3d-photo.jpg", "files/0a1b2c3d-photo.jpg")]
        [InlineData("https://therexnow-bucket.s3.us-west-1.amazonaws.com/files/0a1b2c3d-my%20photo.jpg", "files/0a1b2c3d-my photo.jpg")]
        [InlineData("photo.jpg", null)]
        [InlineData("files/nested/0a1b2c3d-photo.jpg", null)]
        [InlineData("https://other-bucket.s3.us-west-1.amazonaws.com/files/0a1b2c3d-photo.jpg", null)]
        [InlineData("/url1", null)]
        [InlineData("['0a1b2c3d-a.mp4', 'legacy.mp4']", "files/0a1b2c3d-a.mp4")]
        public void MediaKeys_AreTakenOnlyFromReferencesThatMapToOneUniqueObject(string stored, string? expectedKey)
        {
            var keys = AccountPurgeService.MediaKeysIn(new[] { stored }, "therexnow-bucket", out _);

            Assert.Equal(expectedKey == null ? Array.Empty<string>() : new[] { expectedKey }, keys);
        }

        [Fact]
        public async Task WebhookPayloads_NamingTheAccountsStripeIds_AreCleared()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "webhook-uid", new MemberOptions { StripeCustomerId = "cus_hook", PurchaseToken = "google-token-1" });

            await using (var ctx = h.NewContext())
            {
                ctx.AddRange(
                    new WebhookLog { Provider = MembershipProvider.Stripe, EventId = "evt_1", Data = "{\"customer\": \"cus_hook\", \"email\": \"a@example.com\"}" },
                    new WebhookLog { Provider = MembershipProvider.Google, Data = "{\"subscriptionNotification\":{\"purchaseToken\":\"google-token-1\"}}" },
                    new WebhookLog { Provider = MembershipProvider.Stripe, EventId = "evt_2", Data = "{\"customer\": \"cus_hook2\"}" });
                await ctx.SaveChangesAsync();
            }

            Assert.Equal(ResponseStatus.Success, await h.Purge.PurgeUserAsync(member, "self:test"));

            await using var check = h.NewContext();
            var logs = check.Set<WebhookLog>().IgnoreQueryFilters().OrderBy(w => w.Id).ToList();
            Assert.Null(logs[0].Data);
            Assert.Equal("evt_1", logs[0].EventId);
            Assert.Null(logs[1].Data);
            Assert.Equal("{\"customer\": \"cus_hook2\"}", logs[2].Data);
        }

        [Fact]
        public async Task Backlog_DryRunReportsWithoutPersonalData_ThenPurgesInBatches()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var live = await h.SeedMemberAsync(catalogue, "still-here-uid");
            var first = await h.SeedMemberAsync(catalogue, "gone-1", new MemberOptions { SoftDeleted = true, StripeCustomerId = "cus_gone", WaitingOrder = true });
            await h.SeedMemberAsync(catalogue, "gone-1", new MemberOptions { SoftDeleted = true });
            await h.SeedMemberAsync(catalogue, "gone-2", new MemberOptions { SoftDeleted = true });
            h.Stripe.Subscriptions.Add(new FakeSubscription { Id = "sub_gone", CustomerId = "cus_gone" });

            var report = await h.Purge.PurgeSoftDeletedAsync(dryRun: true, max: 0, requestedBy: "admin:test");

            Assert.Equal(ResponseStatus.Success, report.Status);
            Assert.DoesNotContain("@", report.Result);
            var json = JObject.Parse(report.Result);
            Assert.Equal(3, json.Value<int>("softDeletedAccounts"));
            Assert.Equal(1, json["stripe"]!.Value<int>("openSubscriptions"));
            Assert.Equal(1, json.Value<int>("accountsWithWaitingOrders"));
            Assert.Empty(h.Stripe.Cancelled);
            Assert.Equal(4, await h.CountAsync<Users>(u => true));

            // The first account's soft-deleted copy goes with it, so the batch of one clears two rows.
            var batch = JObject.Parse((await h.Purge.PurgeSoftDeletedAsync(dryRun: false, max: 1, requestedBy: "admin:test")).Result);
            Assert.Equal(1, ((JArray)batch["processed"]!).Count);
            Assert.Equal(1, batch.Value<int>("remaining"));
            Assert.Null(await h.FindAsync<Users>(first));
            Assert.Equal(new[] { "sub_gone" }, h.Stripe.Cancelled);

            var rest = JObject.Parse((await h.Purge.PurgeSoftDeletedAsync(dryRun: false, max: 5, requestedBy: "admin:test")).Result);
            Assert.Equal(0, rest.Value<int>("remaining"));
            Assert.NotNull(await h.FindAsync<Users>(live));
        }

        [Fact]
        public async Task DeleteMutation_FollowsTheSelfAndAdminRules()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var adminId = await h.SeedAdminAsync("admin-uid");
            var otherAdminId = await h.SeedAdminAsync("admin-2-uid");
            var memberA = await h.SeedMemberAsync(catalogue, "member-a");
            var memberB = await h.SeedMemberAsync(catalogue, "member-b");
            var memberC = await h.SeedMemberAsync(catalogue, "member-c");
            var memberD = await h.SeedMemberAsync(catalogue, "member-d");
            var mutations = new UserMutations();

            async Task<ResponseStatus> Delete(int callerId, string uid, int? userId)
            {
                var caller = await h.FindAsync<Users>(callerId);
                return (await mutations.Delete(SignedIn(uid, caller), h.Purge, userId)).Status;
            }

            // A member may not delete someone else.
            Assert.Equal(ResponseStatus.NotAllowd, await Delete(memberA, "member-a", memberB));
            Assert.NotNull(await h.FindAsync<Users>(memberB));

            // Their own account, with or without their id (the web app sends it).
            Assert.Equal(ResponseStatus.Success, await Delete(memberA, "member-a", null));
            Assert.Null(await h.FindAsync<Users>(memberA));
            Assert.Equal(ResponseStatus.Success, await Delete(memberB, "member-b", memberB));
            Assert.Null(await h.FindAsync<Users>(memberB));

            // An admin may delete a member, never another admin.
            Assert.Equal(ResponseStatus.Success, await Delete(adminId, "admin-uid", memberC));
            Assert.Null(await h.FindAsync<Users>(memberC));
            Assert.Equal(ResponseStatus.NotAllowd, await Delete(adminId, "admin-uid", otherAdminId));
            Assert.NotNull(await h.FindAsync<Users>(otherAdminId));

            // A valid token whose rows are already gone finishes the job instead of failing.
            var orphan = await mutations.Delete(SignedIn("orphan-uid", row: null), h.Purge, null);
            Assert.Equal(ResponseStatus.Success, orphan.Status);
            Assert.Contains("orphan-uid", h.Firebase.Deleted);
            Assert.NotNull(await h.FindAsync<Users>(memberD));
        }

        [Fact]
        public async Task UserServiceRemove_DeletesPermanently()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "remove-uid");

            var response = h.Resolve<IUserService>().Remove(member);

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(0, await h.CountAsync<Users>(u => u.Id == member));
        }

        [Fact]
        public async Task SignUp_WithALoginThatNoLongerExists_CreatesNoAccount()
        {
            await using var h = new PurgeHarness();
            h.Firebase.Missing.Add("deleted-login-uid");

            var response = h.Resolve<IUserService>().SignUp("deleted-login-uid", "deleted@example.com", new UserInput { FullName = "Replayed token" });

            Assert.Equal(ResponseStatus.UserNotFound, response.Status);
            Assert.Equal(0, await h.CountAsync<Users>(u => u.ExternalId == "deleted-login-uid"));
        }

        [Fact]
        public async Task GetByExternalId_FindsAnAccountThroughItsLinkedLogin()
        {
            await using var h = new PurgeHarness();
            var catalogue = await h.SeedCatalogueAsync();
            var member = await h.SeedMemberAsync(catalogue, "password-uid", new MemberOptions { LinkedUid = "google-uid" });

            var response = h.Resolve<IUserService>().GetByExternalId("google-uid");

            Assert.Equal(ResponseStatus.Success, response.Status);
            Assert.Equal(member, response.Result.Id);
        }

        private static ClaimsPrincipal SignedIn(string uid, Users? row) =>
            new(new ClaimsIdentity(new List<Claim> { new("user_id", uid), new("user", JsonConvert.SerializeObject(row)) }, "Firebase"));
    }
}
