namespace Aps.Apps.HepNow.Repositories;

public class WebhookLogRepository : Repository<WebhookLog, DataContext>, IWebhookLogRepository
{
    public WebhookLogRepository(IDbContextFactory<DataContext> dbContextFactory) : base(dbContextFactory)
    {
    }
}
