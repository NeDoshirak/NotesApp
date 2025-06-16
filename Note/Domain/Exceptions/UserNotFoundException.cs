namespace Domain.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string login)
        : base($"User with login {login} not found.") { }
}