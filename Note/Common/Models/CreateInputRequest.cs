using Microsoft.AspNetCore.Http;

namespace Common.Models;

public class CreateInputRequest
{
    public Guid UserId { get; set; }
    public string? Text { get; set; }
    public IFormFile? WavFile { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}