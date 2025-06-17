using System;

namespace Domain.Entities;

public class Task
{
    public int TaskId { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Location { get; set; }
    public DateTime? DueTime { get; set; }
    public bool IsCompleted { get; set; } = false;
}