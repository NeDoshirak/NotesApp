namespace Application.DTOs;

public class NoteResponse
{
    public string TranscribedText { get; set; } = string.Empty;
    public NoteResult Result { get; set; } = null!;
}

public class NoteResult
{
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? DateTime { get; set; }
    public string? Location { get; set; }
}