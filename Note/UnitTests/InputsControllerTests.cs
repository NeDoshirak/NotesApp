using Application.DTOs;
using Application.Interfaces;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using Presentation.Web.Controllers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.UnitTests.Presentation.Web.Controllers;

public class InputsControllerTests
{
    private readonly Mock<ITaskService> _taskServiceMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly InputsController _controller;
    private readonly ConcurrentDictionary<Guid, (int TaskId, Guid UserId)> _pendingNotes;

    public InputsControllerTests()
    {
        _taskServiceMock = new Mock<ITaskService>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        _controller = new InputsController(_taskServiceMock.Object, _httpClient);
        _pendingNotes = new ConcurrentDictionary<Guid, (int TaskId, Guid UserId)>();
        typeof(InputsController).GetField("_pendingNotes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?.SetValue(null, _pendingNotes);
    }

    [Fact]
    public async Task UploadText_ValidRequestWithLocation_ReturnsOkWithLocationRequired()
    {
        // Arrange
        var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var request = new CreateInputRequest
        {
            UserId = userId,
            Text = "Записаться к стоматологу на пятницу в 10:30"
        };
        var parsedData = new ParsedData
        {
            Category = "Здоровье",
            DateTime = "2025-06-19, 10:30",
            Location = "Стоматологическая клиника",
            Text = "Записаться на пятницу в 10:30 к стоматологу",
            Title = "Записаться к стоматологу"
        };
        var responseContent = JsonSerializer.Serialize(parsedData);
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });
        _taskServiceMock.Setup(s => s.CreateTask(It.IsAny<TaskDto>()))
            .Callback<TaskDto>(t => t.TaskId = 1);

        // Act
        var result = _controller.UploadText(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.Equal("Location required", response.Message);
        Assert.NotNull(response.NoteId);
        Assert.Equal(parsedData.Category, response.ParsedData.category);
        Assert.Single(_pendingNotes);
        _taskServiceMock.Verify(s => s.CreateTask(It.Is<TaskDto>(t => t.UserId == userId && t.LocationName == "Стоматологическая клиника")), Times.Once());
    }

    [Fact]
    public async Task UploadText_ValidRequestWithoutLocation_ReturnsOkWithTaskCreated()
    {
        // Arrange
        var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var request = new CreateInputRequest
        {
            UserId = userId,
            Text = "Купить молоко"
        };
        var parsedData = new ParsedData
        {
            Category = "Покупки",
            DateTime = null,
            Location = null,
            Text = "Купить молоко",
            Title = "Покупка молока"
        };
        var responseContent = JsonSerializer.Serialize(parsedData);
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });
        _taskServiceMock.Setup(s => s.CreateTask(It.IsAny<TaskDto>()))
            .Callback<TaskDto>(t => t.TaskId = 1);

        // Act
        var result = _controller.UploadText(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.Equal("Task created successfully", response.Message);
        Assert.Null(response.NoteId);
        Assert.Empty(_pendingNotes);
        _taskServiceMock.Verify(s => s.CreateTask(It.Is<TaskDto>(t => t.UserId == userId && t.LocationName == null)), Times.Once());
    }

    [Fact]
    public async Task UploadText_EmptyText_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateInputRequest
        {
            UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Text = ""
        };

        // Act
        var result = _controller.UploadText(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Valid user ID and text content are required.", error.Error);
        _taskServiceMock.Verify(s => s.CreateTask(It.IsAny<TaskDto>()), Times.Never());
    }

    [Fact]
    public async Task UploadText_FlaskError_ReturnsErrorStatusCode()
    {
        // Arrange
        var request = new CreateInputRequest
        {
            UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            Text = "Записаться к стоматологу"
        };
        var errorResponse = new FlaskErrorResponse { Error = "API error", StatusCode = 500, ResponseText = "Internal error" };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(JsonSerializer.Serialize(errorResponse))
            });

        // Act
        var result = _controller.UploadText(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        var response = statusCodeResult.Value as dynamic;
        Assert.Equal("API error", response.Error);
        _taskServiceMock.Verify(s => s.CreateTask(It.IsAny<TaskDto>()), Times.Never());
    }

    [Fact]
    public async Task UploadAudio_ValidRequestWithLocation_ReturnsOkWithLocationRequired()
    {
        // Arrange
        var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var fileMock = new Mock<IFormFile>();
        var fileContent = new byte[100];
        var memoryStream = new MemoryStream(fileContent);
        fileMock.Setup(f => f.FileName).Returns("audio.mp3");
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(memoryStream);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(async (Stream target, CancellationToken ct) =>
            {
                await memoryStream.CopyToAsync(target, ct);
                memoryStream.Position = 0; // Reset for reuse
            });
        var request = new CreateInputRequest
        {
            UserId = userId,
            AudioFile = fileMock.Object
        };
        var flaskResponse = new FlaskAudioResponse
        {
            TranscribedText = "Записаться к стоматологу",
            Result = new ParsedData
            {
                Category = "Здоровье",
                DateTime = "2025-06-19, 10:30",
                Location = "Стоматологическая клиника",
                Text = "Записаться на пятницу в 10:30 к стоматологу",
                Title = "Записаться к стоматологу"
            }
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(flaskResponse))
            });
        _taskServiceMock.Setup(s => s.CreateTask(It.IsAny<TaskDto>()))
            .Callback<TaskDto>(t => t.TaskId = 1);

        // Act
        var result = _controller.UploadAudio(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.Equal("Location required", response.Message);
        Assert.NotNull(response.NoteId);
        Assert.Equal(flaskResponse.Result.Category, response.ParsedData.category);
        Assert.Single(_pendingNotes);
        _taskServiceMock.Verify(s => s.CreateTask(It.Is<TaskDto>(t => t.UserId == userId && t.LocationName == "Стоматологическая клиника")), Times.Once());
    }

    [Fact]
    public async Task UploadAudio_ValidRequestWithoutLocation_ReturnsOkWithTaskCreated()
    {
        // Arrange
        var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var fileMock = new Mock<IFormFile>();
        var fileContent = new byte[100];
        var memoryStream = new MemoryStream(fileContent);
        fileMock.Setup(f => f.FileName).Returns("audio.mp3");
        fileMock.Setup(f => f.Length).Returns(fileContent.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(memoryStream);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(async (Stream target, CancellationToken ct) =>
            {
                await memoryStream.CopyToAsync(target, ct);
                memoryStream.Position = 0; // Reset for reuse
            });
        var request = new CreateInputRequest
        {
            UserId = userId,
            AudioFile = fileMock.Object
        };
        var flaskResponse = new FlaskAudioResponse
        {
            TranscribedText = "Купить молоко",
            Result = new ParsedData
            {
                Category = "Покупки",
                DateTime = null,
                Location = null,
                Text = "Купить молоко",
                Title = "Покупка молока"
            }
        };
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(flaskResponse))
            });
        _taskServiceMock.Setup(s => s.CreateTask(It.IsAny<TaskDto>()))
            .Callback<TaskDto>(t => t.TaskId = 1);

        // Act
        var result = _controller.UploadAudio(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.Equal("Task created successfully", response.Message);
        Assert.Null(response.NoteId);
        Assert.Empty(_pendingNotes);
        _taskServiceMock.Verify(s => s.CreateTask(It.Is<TaskDto>(t => t.UserId == userId && t.LocationName == null)), Times.Once());
    }

    [Fact]
    public async Task UploadAudio_NullFile_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateInputRequest
        {
            UserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
            AudioFile = null
        };

        // Act
        var result = _controller.UploadAudio(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Valid user ID and audio file are required.", error.Error);
        _taskServiceMock.Verify(s => s.CreateTask(It.IsAny<TaskDto>()), Times.Never());
    }

    [Fact]
    public async Task SubmitLocation_ValidCoordinates_UpdatesTaskAndReturnsOk()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var taskId = 1;
        _pendingNotes.TryAdd(noteId, (taskId, userId));
        var location = new LocationDto { Latitude = 55.7558, Longitude = 37.6173 };
        var task = new TaskDto
        {
            TaskId = taskId,
            UserId = userId,
            LocationName = "Стоматологическая клиника"
        };
        _taskServiceMock.Setup(s => s.GetAllTasks(userId)).Returns(new List<TaskDto> { task });
        _taskServiceMock.Setup(s => s.UpdateTask(It.IsAny<TaskDto>()));

        // Act
        var result = _controller.SubmitLocation(noteId, location);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value as dynamic;
        Assert.Equal("Task updated with location", response.Message);
        Assert.Empty(_pendingNotes);
        _taskServiceMock.Verify(s => s.UpdateTask(It.Is<TaskDto>(t => t.Location == "Стоматологическая клиника (55.7558, 37.6173)")), Times.Once());
    }

    [Fact]
    public async Task SubmitLocation_InvalidLatitude_ReturnsBadRequest()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var location = new LocationDto { Latitude = 100, Longitude = 37.6173 };

        // Act
        var result = _controller.SubmitLocation(noteId, location);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Latitude must be between -90 and 90 degrees.", error.Error);
        _taskServiceMock.Verify(s => s.UpdateTask(It.IsAny<TaskDto>()), Times.Never());
    }

    [Fact]
    public async Task SubmitLocation_NonExistentNoteId_ReturnsBadRequest()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var location = new LocationDto { Latitude = 55.7558, Longitude = 37.6173 };

        // Act
        var result = _controller.SubmitLocation(noteId, location);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var error = badRequestResult.Value as dynamic;
        Assert.Equal("Note not found or already processed.", error.Error);
        _taskServiceMock.Verify(s => s.UpdateTask(It.IsAny<TaskDto>()), Times.Never());
    }
}