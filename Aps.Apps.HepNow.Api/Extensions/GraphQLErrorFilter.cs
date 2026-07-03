using HotChocolate;

namespace Aps.Apps.HepNow.Extensions;

public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        _logger.LogError(error.Exception, "GraphQL execution error: {Message}", error.Message);
        return error;
    }
}
