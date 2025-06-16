using Application.DTOs;
using Application.Interfaces;
using Common.Models;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Presentation.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterUserDto dto)
    {
        if (string.IsNullOrEmpty(dto.Username) ||
            string.IsNullOrEmpty(dto.Email) ||
            string.IsNullOrEmpty(dto.Password))
        {
            return BadRequest("Все поля обязательны для заполнения");
        }

        try
        {
            var (user, token) = _authService.Register(dto);
            return Ok(new AuthResponse
            {
                User = user,
                Token = token
            });
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Ошибка регистрации: {ex.Message}\n{ex.StackTrace}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка регистрации: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginUserDto dto)
    {
        if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
        {
            return BadRequest("Имя пользователя и пароль обязательны для заполнения");
        }

        try
        {
            var (user, token) = _authService.Login(dto);
            return Ok(new AuthResponse
            {
                User = user,
                Token = token
            });
        }
        catch (UserNotFoundException ex)
        {
            Console.WriteLine($"Ошибка входа: {ex.Message}\n{ex.StackTrace}");
            return Unauthorized("Неверное имя пользователя или пароль");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка входа: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}