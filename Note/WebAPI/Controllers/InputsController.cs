using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Presentation.Web.Controllers;

[ApiController]
[Route("api/inputs")]
public class InputsController : ControllerBase
{
    private readonly IInputService _inputService;
    private readonly HttpClient _httpClient;
    private const string FlaskApiUrl = "http://localhost:5000/parse_note";

    public InputsController(IInputService inputService, HttpClient httpClient)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    [HttpPost("text")]
    public async Task<IActionResult> UploadText([FromForm] CreateInputRequest request)
    {
        try
        {
            if (request.UserId == Guid.Empty || string.IsNullOrEmpty(request.Text))
                throw new ArgumentException("Valid user ID and text content are required.");

            // Prepare JSON payload for Flask API
            var payload = new { note = request.Text };
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            // Send request to Flask API
            var response = await _httpClient.PostAsync(FlaskApiUrl, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return BadRequest(new
                {
                    Error = "API request failed",
                    StatusCode = (int)response.StatusCode,
                    ResponseText = errorContent
                });
            }

            // Parse successful response
            var responseContent = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonSerializer.Deserialize<object>(responseContent);

            // Optionally save to local service
            var dto = new CreateInputDto
            {
                UserId = request.UserId,
                Text = request.Text,
                FileName = "text.txt"
            };
            _inputService.SaveInput(dto);

            return Ok(new { Message = "Text processed successfully", ParsedData = parsedJson });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки текста: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("audio")]
    public async Task<IActionResult> UploadAudio([FromForm] CreateInputRequest request)
    {
        try
        {
            if (request.UserId == Guid.Empty || request.WavFile == null)
                throw new ArgumentException("Valid user ID and audio file are required.");

            using var form = new MultipartFormDataContent();
            using var stream = request.WavFile.OpenReadStream();
            using var content = new StreamContent(stream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.WavFile.ContentType);

            form.Add(content, "audio", request.WavFile.FileName);

            var response = await _httpClient.PostAsync("http://localhost:5000/upload_wav", form);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return BadRequest(new
                {
                    Error = "API request failed",
                    StatusCode = (int)response.StatusCode,
                    ResponseText = errorContent
                });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var parsedJson = JsonSerializer.Deserialize<object>(responseContent);

            using var memoryStream = new MemoryStream();
            await request.WavFile.CopyToAsync(memoryStream);

            var dto = new CreateInputDto
            {
                UserId = request.UserId,
                WavContent = memoryStream.ToArray(),
                FileName = request.WavFile.FileName
            };
            _inputService.SaveInput(dto);

            return Ok(new { Message = "Audio file processed successfully", ParsedData = parsedJson });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio upload error: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }
}
