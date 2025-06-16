namespace Common.Models;
using Microsoft.AspNetCore.Http;

public class CreateInputRequest
{
    public int UserId { get; set; }
    public string? TextContent { get; set; }
    public IFormFile? WavFile { get; set; }
}