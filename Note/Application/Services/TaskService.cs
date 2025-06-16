using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories.Interfaces;

namespace Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    public List<TaskDto> GetAllTasks(int userId)
    {
        var tasks = _taskRepository.GetAllByUserId(userId);
        return tasks.Select(MapToTaskDto).ToList();
    }

    private TaskDto MapToTaskDto(Domain.Entities.Task task)
    {
        return new TaskDto
        {
            Id = task.Id,
            UserId = task.UserId,
            Text = task.Text,
            Category = task.Category,
            CreatedAt = task.CreatedAt,
            Location = task.Location,
            IsCompleted = task.IsCompleted,
            NotificationTime = task.NotificationTime,
            NotificationLocation = task.NotificationLocation
        };
    }
}