using Application.DTOs;
using Application.Interfaces;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using System;


namespace Presentation.Web.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
    }

    [HttpGet("user/{userId}")]
    public IActionResult GetTasks(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
                throw new ArgumentException("Valid user ID is required.");

            var tasks = _taskService.GetAllTasks(userId);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка получения задач: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPut("{taskId}/complete")]
    public IActionResult UpdateTaskCompletion(int taskId, [FromBody] UpdateTaskCompletionDto request)
    {
        try
        {
            _taskService.UpdateTaskCompletion(taskId, request.IsCompleted);
            return Ok(new { Message = "Task completion status updated" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обновления статуса задачи: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }
}

