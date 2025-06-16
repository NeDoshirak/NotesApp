using Application.DTOs;

namespace Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
}