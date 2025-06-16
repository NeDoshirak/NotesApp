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
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Username and password are required.");

        var existingUser = _userRepository.GetByUsername(dto.Username);
        if (existingUser != null)
            throw new ArgumentException($"Username '{dto.Username}' is already taken.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.HashPassword(dto.Password)
        };

        var createdUser = _userRepository.Create(user);
        var userDto = MapToUserDto(createdUser);
        var token = _jwtTokenService.GenerateToken(userDto);

        return (userDto, token);
    }

    public (UserDto User, string Token) Login(LoginUserDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Username and password are required.");

        var user = _userRepository.GetByUsername(dto.Username);
        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            throw new UserNotFoundException(dto.Username);

        var userDto = MapToUserDto(user);
        var token = _jwtTokenService.GenerateToken(userDto);

        return (userDto, token);
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email
        };
    }
}