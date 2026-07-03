using Aps.Apps.HepNow.Models.Dtos;

namespace Aps.Apps.HepNow.Repositories;

public class UserRepository : UserRepositoryBase<DataContext, Users>, IUserRepository
{
    public UserRepository(IDbContextFactory<DataContext> dbContextFactory) : base(dbContextFactory)
    {
    }

    #region functions
    public IQueryable<UserDto> GetUsers()
    {
        return GetDbSet().Where(u => u.UserType != UserType.Admin).Select(a => new UserDto
        {
            User = a,
            Plan = a.Memberships.Where(m => m.StartDate < DateTime.UtcNow && m.EndDate > DateTime.UtcNow).Select(a => a.Plan).FirstOrDefault(),
        });
    }
    #endregion
}
