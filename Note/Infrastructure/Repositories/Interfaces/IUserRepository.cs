using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces;

public interface IUserRepository
{
    User? GetByUsername(string username);
    User Create(User user);
}