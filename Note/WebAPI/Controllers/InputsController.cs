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

    [HttpPost("wav")]
    public async Task<IActionResult> UploadWav([FromForm] CreateInputRequest request)
    {
        try
        {
            if (request.UserId == Guid.Empty || request.WavFile == null)
                throw new ArgumentException("Valid user ID and WAV file are required.");

            if (!request.WavFile.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only WAV files are allowed.");

            // For WAV files, we'll assume the Flask API accepts base64-encoded content
            using var memoryStream = new MemoryStream();
            await request.WavFile.CopyToAsync(memoryStream);
            var wavBytes = memoryStream.ToArray();
            var base64Wav = Convert.ToBase64String(wavBytes);

            // Prepare JSON payload for Flask API
            var payload = new { note = base64Wav };
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
                WavContent = wavBytes,
                FileName = request.WavFile.FileName
            };
            _inputService.SaveInput(dto);

            return Ok(new { Message = "WAV file processed successfully", ParsedData = parsedJson });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки WAV: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }
}