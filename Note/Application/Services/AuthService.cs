using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Repositories.Interfaces;

namespace Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    public (UserDto User, string Token) Register(RegisterUserDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Login, name, and password are required.");

        var existingUser = _userRepository.GetByLogin(dto.Login);
        if (existingUser != null)
            throw new ArgumentException($"Login '{dto.Login}' is already taken.");

        var user = new User
        {
            Login = dto.Login,
            Name = dto.Name,
            PasswordHash = _passwordHasher.HashPassword(dto.Password)
        };

        var createdUser = _userRepository.Create(user);
        var userDto = MapToUserDto(createdUser);
        var token = _jwtTokenService.GenerateToken(userDto);

        return (userDto, token);
    }

    public (UserDto User, string Token) Login(LoginUserDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Login) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Login and password are required.");

        var user = _userRepository.GetByLogin(dto.Login);
        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UserNotFoundException(dto.Login);

        var userDto = MapToUserDto(user);
        var token = _jwtTokenService.GenerateToken(userDto);

        return (userDto, token);
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Login = user.Login,
            Name = user.Name
        };
    }
}