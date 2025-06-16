using Application.DTOs;
using Application.Interfaces;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
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

            var dto = new CreateInputDto
            {
                UserId = request.UserId,
                TextContent = request.TextContent,
                FileName = "text.txt"
            };

            _inputService.SaveInput(dto);
            return Ok(new { Message = "Text uploaded successfully" });
        }
        catch (Exception ex)
        {
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

            using var memoryStream = new MemoryStream();
            request.WavFile.CopyTo(memoryStream);

            var dto = new CreateInputDto
            {
                UserId = request.UserId,
                WavContent = memoryStream.ToArray(),
                FileName = request.WavFile.FileName
            };

            _inputService.SaveInput(dto);
            return Ok(new { Message = "WAV file uploaded successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}