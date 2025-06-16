namespace Application.DTOs;

public class UserDto
{
    public Guid UserId { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}