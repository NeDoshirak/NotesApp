using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Presentation.Web.Controllers;

[ApiController]
[Route("api/inputs")]
public class InputsController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly HttpClient _httpClient;
    private static readonly ConcurrentDictionary<Guid, (int TaskId, Guid UserId)> _pendingNotes = new();
    private const string FlaskApiUrl = "http://localhost:5000";
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public InputsController(ITaskService taskService, HttpClient httpClient)
    {
        _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    [HttpPost("text")]
    public IActionResult UploadText([FromForm] CreateInputRequest request)
    {
        try
        {
            if (request.UserId == Guid.Empty || string.IsNullOrEmpty(request.Text))
                throw new ArgumentException("Valid user ID and text content are required.");

            var requestBody = new { note = request.Text };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = _httpClient.PostAsync($"{FlaskApiUrl}/parse_note", content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine($"Flask response (text): {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FlaskErrorResponse>(responseContent, _jsonOptions);
                return StatusCode((int)response.StatusCode, new { errorResponse?.Error, errorResponse?.StatusCode, errorResponse?.ResponseText });
            }

            var parsedData = JsonSerializer.Deserialize<ParsedData>(responseContent, _jsonOptions);
            if (parsedData == null)
            {
                Console.WriteLine("Ошибка: parsedData равно null.");
                return BadRequest(new { Error = "Invalid response from Flask API" });
            }

            return HandleParsedData(parsedData, request.UserId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки текста: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("audio")]
    public IActionResult UploadAudio([FromForm] CreateInputRequest request)
    {
        try
        {
            if (request.UserId == Guid.Empty || request.AudioFile == null)
                throw new ArgumentException("Valid user ID and audio file are required.");

            using var memoryStream = new MemoryStream();
            request.AudioFile.CopyTo(memoryStream);
            var audioContent = memoryStream.ToArray();

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(audioContent);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "audio", request.AudioFile.FileName);

            var response = _httpClient.PostAsync($"{FlaskApiUrl}/upload_wav", content).Result;
            var responseContent = response.Content.ReadAsStringAsync().Result;

            Console.WriteLine($"Flask response (audio): {responseContent}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = JsonSerializer.Deserialize<FlaskErrorResponse>(responseContent, _jsonOptions);
                return StatusCode((int)response.StatusCode, new { errorResponse?.Error, errorResponse?.StatusCode, errorResponse?.ResponseText });
            }

            var flaskResponse = JsonSerializer.Deserialize<FlaskAudioResponse>(responseContent, _jsonOptions);
            if (flaskResponse == null || flaskResponse.Result == null)
            {
                Console.WriteLine("Ошибка: flaskResponse или Result равны null.");
                return BadRequest(new { Error = "Invalid response from Flask API" });
            }

            var parsedData = flaskResponse.Result;
            return HandleParsedData(parsedData, request.UserId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки аудио: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("location/{noteId}")]
    public IActionResult SubmitLocation(Guid noteId, [FromBody] LocationDto location)
    {
        try
        {
            if (location.Latitude < -90 || location.Latitude > 90)
                throw new ArgumentException("Latitude must be between -90 and 90 degrees.");
            if (location.Longitude < -180 || location.Longitude > 180)
                throw new ArgumentException("Longitude must be between -180 and 180 degrees.");

            if (!_pendingNotes.TryRemove(noteId, out var pendingNote))
                throw new ArgumentException("Note not found or already processed.");

            var taskId = pendingNote.TaskId;
            var userId = pendingNote.UserId;
            var locationWithCoords = $"Стоматологическая клиника ({location.Latitude}, {location.Longitude})";

            var tasks = _taskService.GetAllTasks(userId);
            var task = tasks.FirstOrDefault(t => t.TaskId == taskId);
            if (task == null)
                throw new ArgumentException("Task not found.");

            task.Location = locationWithCoords;
            _taskService.UpdateTask(task);

            return Ok(new { Message = "Task updated with location", Task = task });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения локации: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    private IActionResult HandleParsedData(ParsedData data, Guid userId)
    {
        if (data == null)
        {
            Console.WriteLine("Ошибка: ParsedData равен null в HandleParsedData.");
            return BadRequest(new { Error = "Invalid parsed data" });
        }

        var taskDto = new TaskDto
        {
            UserId = userId,
            Name = data.Title,
            Text = data.Text,
            Category = data.Category,
            Location = null,
            LocationName = data.Location,
            DueTime = string.IsNullOrEmpty(data.DateTime)
                ? null
                : DateTime.TryParse(data.DateTime, out var dueTime)
                    ? dueTime
                    : null,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _taskService.CreateTask(taskDto);

        if (!string.IsNullOrEmpty(data.Location))
        {
            var noteId = Guid.NewGuid();
            _pendingNotes.TryAdd(noteId, (taskDto.TaskId, userId));
            return Ok(new
            {
                Message = "Location required",
                NoteId = noteId,
                ParsedData = data,
                Action = "Request user to provide coordinates"
            });
        }

        return Ok(new { Message = "Task created successfully", Task = taskDto });
    }
}

public class CreateInputRequest
{
    public Guid UserId { get; set; }
    public string? Text { get; set; }
    public IFormFile? AudioFile { get; set; }
}

public class LocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class FlaskErrorResponse
{
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
    public string? ResponseText { get; set; }
}

public class FlaskAudioResponse
{
    [JsonPropertyName("transcribed_text")]
    public string TranscribedText { get; set; } = string.Empty;
    [JsonPropertyName("result")]
    public ParsedData Result { get; set; } = null!;
}

public class ParsedData
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    [JsonPropertyName("date_time")]
    public string? DateTime { get; set; }
    [JsonPropertyName("location")]
    public string? Location { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
}