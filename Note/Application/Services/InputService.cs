using Application.DTOs;
using Application.Interfaces;

namespace Application.Services;

public class InputService : IInputService
{
    private readonly string _uploadPath;

    public InputService(string uploadPath)
    {
        _uploadPath = uploadPath ?? throw new ArgumentNullException(nameof(uploadPath));
        Directory.CreateDirectory(_uploadPath);
    }

    public void SaveInput(CreateInputDto dto)
    {
        if (dto.UserId == Guid.Empty || (dto.Text == null && dto.WavContent == null))
            throw new ArgumentException("Valid user ID and either text or WAV content are required.");

        if (dto.Text != null)
        {
            var fileName = $"text_input_{dto.UserId}_{Guid.NewGuid()}.txt";
            var filePath = Path.Combine(_uploadPath, fileName);
            File.WriteAllText(filePath, dto.Text.Trim());
        }
        else
        {
            var fileName = $"wav_input_{dto.UserId}_{Guid.NewGuid()}{Path.GetExtension(dto.FileName)}";
            var filePath = Path.Combine(_uploadPath, fileName);
            File.WriteAllBytes(filePath, dto.WavContent);
        }
    }
}