namespace Application.DTOs;

public class CreateInputDto
{
    public int UserId { get; set; }
    public string? TextContent { get; set; }
    public byte[]? WavContent { get; set; }
    public string FileName { get; set; } = string.Empty;
}