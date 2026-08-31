using Aps.Apps.HepNow.Data;
using Aps.Apps.HepNow.Extensions;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aps.Apps.HepNow.Test
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", false, true) //use appsettings in main project
                .Build();

            services.AddSingleton<IConfiguration>(config);

            services.AddPooledDbContextFactory<DataContext>(options => options.UseInMemoryDatabase("therexnow-test-db"));

            // UserService reads the caller's token claims to decide whether an account may be
            // claimed by email. The app registers this in AddServices, which the tests bypass.
            services.AddHttpContextAccessor();

            IServiceExtensions.AddRepositoryAndService(services);
            services.AddScoped<ITopicEventSender, UnitTestTopicEventSender>();
        }
    }

    public class UnitTestTopicEventSender : ITopicEventSender
    {
        public ValueTask CompleteAsync(string topic)
        {
            return default(ValueTask); //No Action
        }

        public ValueTask SendAsync<TMessage>(string topic, TMessage message, CancellationToken cancellationToken = default)
        {
            return default(ValueTask); //No Action
        }
    }
}
