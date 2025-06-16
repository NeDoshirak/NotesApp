namespace Application.DTOs;

public class TaskDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Location { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? NotificationTime { get; set; }
    public string? NotificationLocation { get; set; }
}