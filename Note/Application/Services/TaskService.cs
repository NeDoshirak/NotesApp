using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
    }

    public List<TaskDto> GetAllTasks(Guid userId)
    {
        var tasks = _taskRepository.GetAllByUserId(userId);
        return tasks.Select(MapToTaskDto).ToList();
    }

    public void CreateTask(TaskDto taskDto)
    {
        var task = new Domain.Entities.Task
        {
            UserId = taskDto.UserId,
            Name = taskDto.Name,
            Text = taskDto.Text,
            Category = taskDto.Category,
            CreatedAt = DateTime.UtcNow,
            Location = taskDto.Location,
            LocationName = taskDto.LocationName,
            DueTime = taskDto.DueTime,
            IsCompleted = taskDto.IsCompleted
        };

        _taskRepository.Create(task);
        taskDto.TaskId = task.TaskId; 
    }

    public void UpdateTaskCompletion(int taskId, bool isCompleted)
    {
        _taskRepository.UpdateIsCompleted(taskId, isCompleted);
    }

    public void UpdateTask(TaskDto taskDto)
    {
        var task = new Domain.Entities.Task
        {
            TaskId = taskDto.TaskId,
            UserId = taskDto.UserId,
            Name = taskDto.Name,
            Text = taskDto.Text,
            Category = taskDto.Category,
            CreatedAt = taskDto.CreatedAt,
            Location = taskDto.Location,
            LocationName = taskDto.LocationName,
            DueTime = taskDto.DueTime,
            IsCompleted = taskDto.IsCompleted
        };

        _taskRepository.UpdateTask(task);
    }

    private TaskDto MapToTaskDto(Domain.Entities.Task task)
    {
        return new TaskDto
        {
            TaskId = task.TaskId,
            UserId = task.UserId,
            Name = task.Name,
            Text = task.Text,
            Category = task.Category,
            CreatedAt = task.CreatedAt,
            Location = task.Location,
            LocationName = task.LocationName,
            DueTime = task.DueTime,
            IsCompleted = task.IsCompleted
        };
    }
}