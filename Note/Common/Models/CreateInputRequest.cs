using Microsoft.AspNetCore.Http;

public class CreateInputRequest
{
    public Guid UserId { get; set; }
    public string? Text { get; set; }
    public IFormFile? AudioFile { get; set; }
}