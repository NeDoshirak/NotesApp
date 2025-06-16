using Microsoft.AspNetCore.Http;

namespace Common.Models;

public class CreateInputRequest
{
    public int UserId { get; set; }
    public string? TextContent { get; set; }
    public IFormFile? WavFile { get; set; }
    public double? Latitude { get; set; } 
    public double? Longitude { get; set; } 
}