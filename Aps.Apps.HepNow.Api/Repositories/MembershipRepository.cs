namespace Aps.Apps.HepNow.Repositories;

public class MembershipRepository
 : Repository<Memberships,DataContext>,IMembershipRepository
{
public MembershipRepository (IDbContextFactory<DataContext> dbContextFactory)
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
