namespace Application.DTOs;

public class NoteResponse
{
    public string TranscribedText { get; set; } = string.Empty;
    public NoteResult Result { get; set; } = null!;
}

