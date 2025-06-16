namespace Application.DTOs;

public class CreateInputDto
{
    public Guid UserId { get; set; }
    public string? Text { get; set; }
    public byte[]? WavContent { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double? Latitude { get; set; } // Широта, от -90 до 90
    public double? Longitude { get; set; } // Долгота, от -180 до 180
}