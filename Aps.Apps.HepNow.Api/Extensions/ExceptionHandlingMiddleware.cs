using System.Net;

namespace Aps.Apps.HepNow.Extensions;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly bool _includeDetails;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _includeDetails = configuration["env"] != "prd";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var body = new
            {
                status = (int)HttpStatusCode.InternalServerError,
                title = "An unexpected error occurred.",
                detail = _includeDetails ? ex.Message : null,
            };

            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(body));
        }
    }
}
