using Domain.Entities;
using System;
using System.Collections.Generic;

namespace Infrastructure.Repositories.Interfaces;

public interface ITaskRepository
{
    List<Domain.Entities.Task> GetAllByUserId(Guid userId);
    void Create(Domain.Entities.Task task);
    void UpdateIsCompleted(int taskId, bool isCompleted);
    void UpdateTask(Domain.Entities.Task task);
}