using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Interfaces;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public UserRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public User? GetByLogin(string login)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT user_id, login, name, password_hash FROM users WHERE login = @login",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("login", login);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                UserId = reader.GetGuid(0),
                Login = reader.GetString(1),
                Name = reader.GetString(2),
                PasswordHash = reader.GetString(3)
            };
        }

        return null;
    }

    public User Create(User user)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "INSERT INTO users (user_id, login, name, password_hash) VALUES (@user_id, @login, @name, @password_hash) RETURNING user_id",
            (NpgsqlConnection)connection);
        var newUserId = Guid.NewGuid();
        command.Parameters.AddWithValue("user_id", newUserId);
        command.Parameters.AddWithValue("login", user.Login);
        command.Parameters.AddWithValue("name", user.Name);
        command.Parameters.AddWithValue("password_hash", user.PasswordHash);

        command.ExecuteScalar();
        user.UserId = newUserId;
        return user;
    }
}