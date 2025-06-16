using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

    [HttpGet]
    public IActionResult GetAllTasks([FromQuery] int userId)
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID.");

            var tasks = _taskService.GetAllTasks(userId);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}