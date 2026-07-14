using EventBoard.Api.Models;
using EventBoard.Api.Repositories;
using EventBoard.Api.Services;
using Moq;
using Xunit;

namespace EventBoard.Api.Tests;

public class EventServiceTests
{
    private readonly Mock<IEventRepository> _repositoryMock;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _repositoryMock = new Mock<IEventRepository>();
        _service = new EventService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateEvent_ValidEvent_ReturnsCreatedEvent()
    {
        // Arrange
        var evt = new Event { Id = 1, Title = "Conference" };

        _repositoryMock
            .Setup(r => r.CreateAsync(evt))
            .ReturnsAsync(evt);

        // Act
        var result = await _service.CreateEventAsync(evt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);

        _repositoryMock.Verify(r => r.CreateAsync(evt), Times.Once);
    }

    [Fact]
    public async Task CreateEvent_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CreateEventAsync(null!));

        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public async Task GetAllEvents_ReturnsAllEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new Event { Id = 1, Title = "A" },
            new Event { Id = 2, Title = "B" }
        };

        _repositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(events);

        // Act
        var result = await _service.GetAllEventsAsync();

        // Assert
        Assert.Equal(2, result.Count());

        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetEventById_ExistingId_ReturnsEvent()
    {
        // Arrange
        var evt = new Event { Id = 5 };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(evt);

        // Act
        var result = await _service.GetEventByIdAsync(5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);

        _repositoryMock.Verify(r => r.GetByIdAsync(5), Times.Once);
    }

    [Fact]
    public async Task GetEventById_InvalidId_ReturnsNull()
    {
        // Arrange

        // Act
        var result = await _service.GetEventByIdAsync(-1);

        // Assert
        Assert.Null(result);

        _repositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetEventById_NonExistingId_ReturnsNull()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(99))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _service.GetEventByIdAsync(99);

        // Assert
        Assert.Null(result);

        _repositoryMock.Verify(r => r.GetByIdAsync(99), Times.Once);
    }

    [Fact]
    public async Task DeleteEvent_ExistingId_ReturnsTrue()
    {
        // Arrange
        var evt = new Event { Id = 10 };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(evt);

        // Act
        var result = await _service.DeleteEventAsync(10);

        // Assert
        Assert.True(result);

        _repositoryMock.Verify(r => r.DeleteAsync(10), Times.Once);
    }

    [Fact]
    public async Task DeleteEvent_NonExistingId_ReturnsFalse()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(50))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _service.DeleteEventAsync(50);

        // Assert
        Assert.False(result);

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteEvent_InvalidId_ReturnsFalse()
    {
        // Arrange

        // Act
        var result = await _service.DeleteEventAsync(0);

        // Assert
        Assert.False(result);

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }
}