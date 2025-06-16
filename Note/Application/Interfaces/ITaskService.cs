using Application.DTOs;

namespace Application.Interfaces;

public interface ITaskService
{
    List<TaskDto> GetAllTasks(int userId);
}