namespace Aps.Apps.HepNow.Extensions;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext, IUserRepository userRepo)
    {
        if (httpContext.User?.Identity is not null && httpContext.User.Identity.IsAuthenticated)
        {
            var externalId = httpContext.User.FindFirstValue("user_id");

            var user = userRepo.Where(a => a.ExternalId == externalId || a.ExternalId2 == externalId).FirstOrDefault();

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

