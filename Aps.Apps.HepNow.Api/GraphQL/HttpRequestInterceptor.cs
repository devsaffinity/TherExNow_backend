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
            if (context.User is not null && context.User.Identity.IsAuthenticated)
            {
                var externalId = context.User.FindFirstValue("user_id");

                var scope = context.RequestServices.CreateScope();
                var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
                var dbContext = contextFactory.CreateDbContext();
                var currentUser = dbContext.Set<Users>().SingleOrDefault(a => a.ExternalId == externalId || a.ExternalId2 == externalId);

                requestBuilder.SetProperty("Authentication", new Authentication(context.User, currentUser));
            }
            else
            {
                requestBuilder.SetProperty("Authentication", new Authentication(context.User, null));
            }

            requestBuilder.SetProperty("HttpContext", context);

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
