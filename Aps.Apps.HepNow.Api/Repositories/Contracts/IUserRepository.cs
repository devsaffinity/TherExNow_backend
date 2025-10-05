using Aps.Apps.HepNow.Models.Dtos;
using Aps.Apps.HepNow.Models.Entities;

namespace Aps.Apps.HepNow.Repositories.Contracts;

public interface IUserRepository: IUserRepositoryBase<Users>
{
    IQueryable<UserDto> GetUsers();
}
