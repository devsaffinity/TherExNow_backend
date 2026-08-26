using Aps.Apps.HepNow.Extensions;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;

namespace Aps.Apps.HepNow.GraphQL
{
    public class HttpRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<HttpRequestInterceptor>>();

            if (context.User is not null && context.User.Identity.IsAuthenticated)
            {
                var externalId = context.User.FindFirstValue("user_id");

                var scope = context.RequestServices.CreateScope();
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
                var dbContext = contextFactory.CreateDbContext();
                var currentUser = dbContext.Set<Users>().SingleOrDefault(a => a.ExternalId == externalId || a.ExternalId2 == externalId);

                // Same rule as AuthMiddleware: only a provider-verified address may select an
                // account. See FirebaseClaims for why an unverified one cannot be trusted here.
                if (currentUser is null && FirebaseClaims.TryGetVerifiedEmail(context.User, out var email))
                {
                    currentUser = dbContext.Set<Users>().SingleOrDefault(a => a.Email == email);

                    if (currentUser is not null)
                    {
                        if (currentUser.ExternalId != externalId && currentUser.ExternalId2 != externalId)
                        {
                            currentUser.ExternalId2 = externalId;
                            dbContext.SaveChanges();
                            logger.LogInformation("Linked additional external id {ExternalId} to user {UserId}", externalId, currentUser.Id);
                        }
                    }
                    else
                    {
                        currentUser = new Users
                        {
                            ExternalId = externalId,
                            Email = email,
                            FullName = context.User.FindFirstValue("name") ?? email,
                            Language = "English",
                            UserType = UserType.NormalUser,
                            // Safe only because the address above is verified.
                            IsConfirmed = true,
                        };

                        try
                        {
                            dbContext.Set<Users>().Add(currentUser);
                            dbContext.SaveChanges();
                        }
                        catch (DbUpdateException ex)
                        {
                            // A client opening the app fires several GraphQL requests at once and
                            // each reaches this branch, so the insert races its siblings. Detach
                            // the rejected entity so the context is usable, then read the winner.
                            logger.LogWarning(ex, "Race creating user for external id {ExternalId}; re-reading", externalId);
                            dbContext.Entry(currentUser).State = EntityState.Detached;
                            currentUser = dbContext.Set<Users>().SingleOrDefault(a => a.Email == email);
                        }
                    }
                }

                if (currentUser is null)
                    logger.LogWarning("Authenticated GraphQL request with no matching local user for external id {ExternalId}", externalId);

                requestBuilder.SetGlobalState("Authentication", new Authentication(context.User, currentUser));
            }
            else
            {
                requestBuilder.SetGlobalState("Authentication", new Authentication(context.User, null));
            }

            requestBuilder.SetGlobalState("HttpContext", context);

            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }

    public class Authentication
    {
        public Authentication(ClaimsPrincipal claims, Users currentUser)
        {
            this.Claims = claims;
            this.CurrentUser = currentUser;
        }

        public ClaimsPrincipal Claims { get; }
        public Users CurrentUser { get; }
        public bool IsAuthenticated => Claims.Identity.IsAuthenticated;
        public bool IsAuthorized => CurrentUser != null;
        public ResponseStatus Status
        {
            get
            {
                if (!IsAuthenticated) return ResponseStatus.AuthenticationFailed;
                if (!IsAuthorized) return ResponseStatus.UserNotFound;
                return ResponseStatus.Success;
            }
        }

        public void Deconstruct(out ClaimsPrincipal Claims, out Users CurrentUser, out ResponseStatus Status)
        {
            Claims = this.Claims;
            CurrentUser = this.CurrentUser;
            Status = this.Status;
        }
    }

    public class AuthenticationAttribute : GlobalStateAttribute
    {
        public AuthenticationAttribute() : base("Authentication") { }
    }

    public class HttpContextAttribute : GlobalStateAttribute
    {
        public HttpContextAttribute() : base("HttpContext") { }
    }
}
