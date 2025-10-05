namespace Aps.Apps.HepNow.Repositories;

public class NotificationRepository
 : Repository<Notification,DataContext>,INotificationRepository
{
public NotificationRepository (IDbContextFactory<DataContext> dbContextFactory)
:base(dbContextFactory)
{
	context = dbContextFactory.CreateDbContext();
}

#region props
	private DataContext context;

#endregion
#region functions
#endregion
}
