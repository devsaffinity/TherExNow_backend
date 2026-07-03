using Aps.Apps.HepNow.Api.GraphQL.Subscriptions;
using Aps.Apps.HepNow.GraphQL;
using Aps.Apps.HepNow.Models.Dtos;
using Apsy.Common.Api.Auth;
using Apsy.Common.Api.Core.ApiDoc;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;

namespace Aps.Apps.HepNow.Extensions;

public static class IServiceExtensions
{
    private static IEnumerable<Type> _allTypes => Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsClass || t.IsInterface);

    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        var env = configuration["env"];
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

        services.AddCors(options => options.AddPolicy("AllowAllOrigins", builder =>
        {
            // dev/qa (or no AllowedOrigins configured yet for stg/prd): keep current
            // wide-open behavior so nothing breaks. Once AllowedOrigins is populated
            // in stg/prd config, this policy automatically locks down to that list.
            if (env == "dev" || env == "qa" || allowedOrigins.Length == 0)
            {
                builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }
            else
            {
                builder.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }
        }));


        var projectName = configuration["ProjectName"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://securetoken.google.com/{projectName}";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"https://securetoken.google.com/{projectName}",
                    ValidateAudience = true,
                    ValidAudience = projectName,
                    ValidateLifetime = true
                };
            });

        services.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });

        services.Configure<IISServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });

        services.Configure<TwilioConfig>(configuration.GetSection("TwilioConfig"));
        services.Configure<SendGridConfig>(configuration.GetSection("SendGridConfig"));
        services.Configure<StripeSettings>(configuration.GetSection("StripeSettings"));
        services.Configure<TranslatorConfig>(configuration.GetSection("TranslatorConfig"));

        services.AddHttpContextAccessor();
        services.AddCors();

        services.AddControllersWithViews();

        services.AddSwaggerGen(s => s.SchemaFilter<SwaggerIgnoreFilter>());

        string connectionString = configuration.GetConnectionString("DbConnection");
        services.AddPooledDbContextFactory<DataContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            // Sensitive data logging echoes real parameter values (PII, tokens) into
            // the log stream - dev-only, never stg/prd.
            if (env == "dev")
            {
                options.UseLoggerFactory(new LoggerFactory(new[] { new DebugLoggerProvider() }))
                    .EnableSensitiveDataLogging();
            }
        });

        #region RegisterServices
        AddRepositoryAndService(services);

        var graphql = services.AddGraphQLServer()
            .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = configuration["env"] != "prd")
            .AddErrorFilter<GraphQLErrorFilter>()
            .AddHttpRequestInterceptor<HttpRequestInterceptor>()
            .AddQueryType<QueryType>()
            .AddMutationType<MutationType>()
            .AddSubscriptionType<SubscriptionType>()
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            .AddAuthorization()
            .AddInMemorySubscriptions();

        AddQueryMutationSubscription(services, graphql);
        #endregion
    }

    public static void AddRepositoryAndService(IServiceCollection services)
    {
        services.AddScoped<IAuthService, FirebaseAuthService>();
        services.AddScoped<FirebaseAppCreator>();

        var repositoryNS = $"{typeof(FirebaseAppCreator).Namespace}.Repositories";
        var serviceNS = $"{typeof(FirebaseAppCreator).Namespace}.Services";

        var repositoryTypes = _allTypes.Where(x => x.Namespace?.StartsWith(repositoryNS) ?? false);
        foreach (var @interface in repositoryTypes.Where(x => x.IsInterface))
        {
            var implementation = repositoryTypes.First(x => x.IsClass && x.IsAssignableTo(@interface));
            services.AddScoped(@interface, implementation);
        }

        var serviceTypes = _allTypes.Where(x => x.Namespace?.StartsWith(serviceNS) ?? false);
        foreach (var @interface in serviceTypes.Where(x => x.IsInterface))
        {
            var implementation = serviceTypes.First(x => x.IsClass && x.IsAssignableTo(@interface));
            services.AddScoped(@interface, implementation);
        }
    }

    public static void AddQueryMutationSubscription(IServiceCollection services, IRequestExecutorBuilder graphql)
    {
        var extendMutations = _allTypes.Where(x => x.Namespace == typeof(Mutation).Namespace)
            .Where(x => x.GetCustomAttribute<ExtendObjectTypeAttribute>() != null);
        foreach (var exm in extendMutations)
        {
            graphql.AddTypeExtension(exm);
        }

        var extendQueries = _allTypes.Where(x => x.Namespace == typeof(Query).Namespace)
            .Where(x => x.GetCustomAttribute<ExtendObjectTypeAttribute>() != null);
        foreach (var exq in extendQueries)
        {
            graphql.AddTypeExtension(exq);
        }

        var extendSubscriptions = _allTypes.Where(x => x.Namespace == typeof(Subscription).Namespace)
          .Where(x => x.GetCustomAttribute<ExtendObjectTypeAttribute>() != null);
        foreach (var exs in extendSubscriptions)
        {
            graphql.AddTypeExtension(exs);
        }
    }
}

