using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Interfaces;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public TaskRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public List<Domain.Entities.Task> GetAllByUserId(int userId)
    {
        var tasks = new List<Domain.Entities.Task>();
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT id, user_id, text, category, created_at, location, is_completed, notification_time, notification_location " +
            "FROM tasks WHERE user_id = @userId",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("userId", userId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(new Domain.Entities.Task
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Text = reader.GetString(2),
                Category = reader.IsDBNull(3) ? null : reader.GetString(3),
                CreatedAt = reader.GetDateTime(4),
                Location = reader.IsDBNull(5) ? null : reader.GetString(5),
                IsCompleted = reader.GetBoolean(6),
                NotificationTime = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
                NotificationLocation = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return tasks;
    }
}