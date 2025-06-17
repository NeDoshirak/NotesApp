using System;

namespace Application.DTOs;

public class CreateInputDto
{
    public Guid UserId { get; set; }
    public string? Text { get; set; }
    public byte[]? WavContent { get; set; }
    public string? FileName { get; set; }
}