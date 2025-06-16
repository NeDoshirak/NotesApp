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

    public User? GetByUsername(string username)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT id, username, email, password_hash FROM users WHERE username = @username",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("username", username);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = reader.IsDBNull(2) ? null : reader.GetString(2),
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
            "INSERT INTO users (username, email, password_hash) VALUES (@username, @email, @password_hash) RETURNING id",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("username", user.Username);
        command.Parameters.AddWithValue("email", user.Email ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("password_hash", user.PasswordHash);

        var id = (int)command.ExecuteScalar();
        user.Id = id;
        return user;
    }
}