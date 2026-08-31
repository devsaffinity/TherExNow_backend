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

