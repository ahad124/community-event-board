using System.Net;
using EventBoard.Api.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using System.Net.Http.Json;

namespace EventBoard.Api.Tests;

public class EventsControllerIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EventsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllEvents_ReturnsOk()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/api/events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
public async Task GetEventById_InvalidId_ReturnsBadRequest()
{
    // Arrange
    var invalidId = 0;

    // Act
    var response = await _client.GetAsync($"/api/events/{invalidId}");

    // Assert
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
[Fact]
public async Task CreateEvent_WithoutToken_ReturnsUnauthorized()
{
    // Arrange
    var request = new
    {
        title = "Integration Test",
        description = "Testing",
        date = DateTime.UtcNow,
        location = "Lahore",
        imageUrl = "",
        categoryId = 1,
        organizerId = Guid.NewGuid()
    };

    // Act
    var response = await _client.PostAsJsonAsync("/api/events", request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
[Fact]
public async Task UpdateEvent_WithoutToken_ReturnsUnauthorized()
{
    // Arrange
    var request = new
    {
        title = "Updated Event"
    };

    // Act
    var response = await _client.PutAsJsonAsync("/api/events/1", request);

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
[Fact]
public async Task DeleteEvent_WithoutToken_ReturnsUnauthorized()
{
    // Act
    var response = await _client.DeleteAsync("/api/events/1");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
[Fact]
public async Task DeleteEvent_WithoutAuthentication_ReturnsUnauthorized()
{
    // Act
    var response = await _client.DeleteAsync("/api/events/1");

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
[Fact]
public async Task GetEventById_NotFound_ReturnsNotFound()
{
    var response = await _client.GetAsync("/api/events/99999");

    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
[Fact]
public async Task GetEventById_NonExistingId_ReturnsNotFound()
{
    // Arrange

    // Act
    var response = await _client.GetAsync("/api/events/99999");

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
}