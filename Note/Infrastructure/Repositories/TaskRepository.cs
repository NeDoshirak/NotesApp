using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Interfaces;
using Npgsql;
using System.Data;
using System.Collections.Generic;

namespace Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public TaskRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    public List<Domain.Entities.Task> GetAllByUserId(Guid userId)
    {
        var tasks = new List<Domain.Entities.Task>();
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "SELECT task_id, user_id, name, text, category, created_at, location, due_time " +
            "FROM tasks WHERE user_id = @userId",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("userId", userId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(new Domain.Entities.Task
            {
                TaskId = reader.GetInt32(0),
                UserId = reader.GetGuid(1),
                Name = reader.GetString(2),
                Text = reader.IsDBNull(3) ? null : reader.GetString(3),
                Category = reader.GetString(4),
                CreatedAt = reader.GetDateTime(5),
                Location = reader.IsDBNull(6) ? null : reader.GetString(6),
                DueTime = reader.IsDBNull(7) ? null : reader.GetDateTime(7)
            });
        }

        return tasks;
    }

}