namespace Domain.Entities;

public class User
{
    public Guid UserId { get; set; } 
    public string Login { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; 
    public string PasswordHash { get; set; } = string.Empty; 
}