using Application.DTOs;
using System.Collections.Generic;

namespace Application.Interfaces;

public interface ITaskService
{
    List<TaskDto> GetAllTasks(Guid userId);
}