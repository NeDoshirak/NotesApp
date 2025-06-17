using Microsoft.AspNetCore.Http;
using System;

namespace Application.DTOs;

public class CreateInputRequest
{
    public Guid UserId { get; set; }
    public string? Text { get; set; }
    public IFormFile? WavFile { get; set; }
}