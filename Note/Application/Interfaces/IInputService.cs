using Application.DTOs;

namespace Application.Interfaces;

public interface IInputService
{
    void SaveInput(CreateInputDto dto);
}