namespace Aps.Apps.HepNow.Repositories;

public class PrintDataRepository : Repository<PrintData,DataContext>, IPrintDataRepository
{
	public PrintDataRepository (IDbContextFactory<DataContext> dbContextFactory) :base(dbContextFactory)
	{
		context = dbContextFactory.CreateDbContext();
	}

	private DataContext context;
}
