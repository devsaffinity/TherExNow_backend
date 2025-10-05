namespace Aps.Apps.HepNow.Repositories;

public class FavoriteExerciseRepository
 : Repository<FavoriteExercises,DataContext>,IFavoriteExerciseRepository
{
public FavoriteExerciseRepository (IDbContextFactory<DataContext> dbContextFactory)
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
