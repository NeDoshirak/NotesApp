using Application.DTOs;
using Application.Interfaces;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Presentation.Web.Controllers;

[ApiController]
[Route("api/inputs")]
public class InputsController : ControllerBase
{
    private readonly IInputService _inputService;

    public InputsController(IInputService inputService)
    {
        _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
    }

    [HttpPost("text")]
    public IActionResult UploadText([FromForm] CreateInputRequest request)
    {
        try
        {
            if (request.UserId <= 0 || string.IsNullOrEmpty(request.TextContent))
                throw new ArgumentException("Valid user ID and text content are required.");

            // Валидация геолокации
            if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
                throw new ArgumentException("Latitude must be between -90 and 90 degrees.");
            if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
                throw new ArgumentException("Longitude must be between -180 and 180 degrees.");

            var dto = new CreateInputDto
            {
                UserId = request.UserId,
                TextContent = request.TextContent,
                FileName = "text.txt",
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            _inputService.SaveInput(dto);
            return Ok(new { Message = "Text uploaded successfully", Latitude = dto.Latitude, Longitude = dto.Longitude });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки текста: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }

    [HttpPost("wav")]
    public IActionResult UploadWav([FromForm] CreateInputRequest request)
    {
        try
        {
            if (request.UserId <= 0 || request.WavFile == null)
                throw new ArgumentException("Valid user ID and WAV file are required.");

            if (!request.WavFile.FileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only WAV files are allowed.");

            // Валидация геолокации
            if (request.Latitude.HasValue && (request.Latitude < -90 || request.Latitude > 90))
                throw new ArgumentException("Latitude must be between -90 and 90 degrees.");
            if (request.Longitude.HasValue && (request.Longitude < -180 || request.Longitude > 180))
                throw new ArgumentException("Longitude must be between -180 and 180 degrees.");

            using var memoryStream = new MemoryStream();
            request.WavFile.CopyTo(memoryStream);

            var dto = new CreateInputDto
            {
                UserId = request.UserId,
                WavContent = memoryStream.ToArray(),
                FileName = request.WavFile.FileName,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            _inputService.SaveInput(dto);
            return Ok(new { Message = "WAV file uploaded successfully", Latitude = dto.Latitude, Longitude = dto.Longitude });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки WAV: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(new { Error = ex.Message });
        }
    }
}