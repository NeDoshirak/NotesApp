using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces;

public interface IUserRepository
{
    User? GetByLogin(string login);
    User Create(User user);
}