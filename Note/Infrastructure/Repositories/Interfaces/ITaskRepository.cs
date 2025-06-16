using Domain.Entities;

namespace Infrastructure.Repositories.Interfaces;

public interface ITaskRepository
{
    List<Domain.Entities.Task> GetAllByUserId(Guid userId);
}