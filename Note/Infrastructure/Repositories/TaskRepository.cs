using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Repositories.Interfaces;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;

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
            "SELECT task_id, user_id, name, text, category, created_at, location, location_name, due_time, is_completed " +
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
                LocationName = reader.IsDBNull(7) ? null : reader.GetString(7),
                DueTime = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                IsCompleted = reader.GetBoolean(9)
            });
        }

        return tasks;
    }

    public void Create(Domain.Entities.Task task)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "INSERT INTO tasks (user_id, name, text, category, created_at, location, location_name, due_time, is_completed) " +
            "VALUES (@user_id, @name, @text, @category, @created_at, @location, @location_name, @due_time, @is_completed) RETURNING task_id",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("user_id", task.UserId);
        command.Parameters.AddWithValue("name", task.Name);
        command.Parameters.AddWithValue("text", (object?)task.Text ?? DBNull.Value);
        command.Parameters.AddWithValue("category", task.Category);
        command.Parameters.AddWithValue("created_at", task.CreatedAt);
        command.Parameters.AddWithValue("location", (object?)task.Location ?? DBNull.Value);
        command.Parameters.AddWithValue("location_name", (object?)task.LocationName ?? DBNull.Value);
        command.Parameters.AddWithValue("due_time", (object?)task.DueTime ?? DBNull.Value);
        command.Parameters.AddWithValue("is_completed", task.IsCompleted);

        var taskId = (int)command.ExecuteScalar();
        task.TaskId = taskId;
    }

    public void UpdateIsCompleted(int taskId, bool isCompleted)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "UPDATE tasks SET is_completed = @is_completed WHERE task_id = @task_id",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("is_completed", isCompleted);
        command.Parameters.AddWithValue("task_id", taskId);

        command.ExecuteNonQuery();
    }

    public void UpdateTask(Domain.Entities.Task task)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();

        using var command = new NpgsqlCommand(
            "UPDATE tasks SET location = @location WHERE task_id = @task_id",
            (NpgsqlConnection)connection);
        command.Parameters.AddWithValue("location", (object?)task.Location ?? DBNull.Value);
        command.Parameters.AddWithValue("task_id", task.TaskId);

        command.ExecuteNonQuery();
    }
}