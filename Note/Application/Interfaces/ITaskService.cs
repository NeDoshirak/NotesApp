using Application.DTOs;
using System;
using System.Collections.Generic;

namespace Application.Interfaces;

public interface ITaskService
{
    List<TaskDto> GetAllTasks(Guid userId);
    void CreateTask(TaskDto taskDto);
    void UpdateTaskCompletion(int taskId, bool isCompleted);
    void UpdateTask(TaskDto taskDto);
}