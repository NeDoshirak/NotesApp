using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    (UserDto User, string Token) Register(RegisterUserDto dto);
    (UserDto User, string Token) Login(LoginUserDto dto);
}