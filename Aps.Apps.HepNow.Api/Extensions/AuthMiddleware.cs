namespace Aps.Apps.HepNow.Extensions;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext, IUserRepository userRepo)
    {
        if (httpContext.User?.Identity is not null && httpContext.User.Identity.IsAuthenticated)
        {
            var externalId = httpContext.User.FindFirstValue("user_id");

            var user = userRepo.Where(a => a.ExternalId == externalId || a.ExternalId2 == externalId).FirstOrDefault();

            // A Firebase UID with no local row is either a first sign-in or a second provider for
            // an existing account. Resolve it by email — but only an address the provider has
            // *verified*, because an unverified one would let anyone who signs up with someone
            // else's address inherit that account. Callers who fail this stay null and fall
            // through to the sign-up flow, exactly as before.
            if (user is null && FirebaseClaims.TryGetVerifiedEmail(httpContext.User, out var email))
            {
                user = userRepo.Where(a => a.Email == email).FirstOrDefault();

                if (user is not null)
                {
                    if (user.ExternalId != externalId && user.ExternalId2 != externalId)
                    {
                        user.ExternalId2 = externalId;
                        userRepo.Update(user);
                        _logger.LogInformation("Linked additional external id {ExternalId} to user {UserId}", externalId, user.Id);
                    }
                }
                else
                {
                    user = new Users
                    {
                        ExternalId = externalId,
                        Email = email,
                        FullName = httpContext.User.FindFirstValue("name") ?? email,
                        Language = "English",
                        UserType = UserType.NormalUser,
                        // Safe only because the address above is verified.
                        IsConfirmed = true,
                    };

                    try
                    {
                        userRepo.Add(user);
                    }
                    catch (DbUpdateException ex)
                    {
                        // A first page load fires several requests at once and every one reaches
                        // this branch, so the insert races its own siblings. Re-read rather than
                        // fail the request: one of them won and the row exists now.
                        _logger.LogWarning(ex, "Race creating user for external id {ExternalId}; re-reading", externalId);
                        user = userRepo.Where(a => a.Email == email).FirstOrDefault();
                    }
                }
            }

            if (user is null)
                _logger.LogWarning("Authenticated request with no matching local user for external id {ExternalId}", externalId);

            var claims = new List<Claim>
                {
                    new Claim("user", JsonConvert.SerializeObject(user))
                };

            var appIdentity = new ClaimsIdentity(claims);
            httpContext.User.AddIdentity(appIdentity);
        }

        await _next(httpContext);
    }
}

