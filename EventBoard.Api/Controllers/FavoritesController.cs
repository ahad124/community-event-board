using System.Security.Claims;
using EventBoard.Api.Models;
using EventBoard.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<FavoritesController> _logger;

    public FavoritesController(
        IFavoriteRepository favoriteRepository,
        IEventRepository eventRepository,
        ILogger<FavoritesController> logger)
    {
        _favoriteRepository = favoriteRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <summary>
    /// Toggle favorite status of an event (Add if not favorited, Remove if already favorited)
    /// </summary>
    [HttpPost("{eventId}")]
    public async Task<IActionResult> ToggleFavorite(int eventId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var @event = await _eventRepository.GetByIdAsync(eventId);
        if (@event == null)
        {
            return NotFound($"Event with ID {eventId} not found.");
        }

        var isFavorited = await _favoriteRepository.ExistsAsync(userId, eventId);
        if (isFavorited)
        {
            await _favoriteRepository.RemoveAsync(userId, eventId);
            _logger.LogInformation("User {UserId} removed event {EventId} from favorites", userId, eventId);
            return Ok(new { IsFavorite = false, Message = "Removed from favorites" });
        }
        else
        {
            var favorite = new EventFavorite
            {
                UserId = userId,
                EventId = eventId,
                AddedAt = DateTime.UtcNow
            };
            await _favoriteRepository.AddAsync(favorite);
            _logger.LogInformation("User {UserId} added event {EventId} to favorites", userId, eventId);
            return Ok(new { IsFavorite = true, Message = "Added to favorites" });
        }
    }

    /// <summary>
    /// Remove an event from favorites
    /// </summary>
    [HttpDelete("{eventId}")]
    public async Task<IActionResult> RemoveFavorite(int eventId)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var exists = await _favoriteRepository.ExistsAsync(userId, eventId);
        if (!exists)
        {
            return NotFound("This event is not in your favorites.");
        }

        await _favoriteRepository.RemoveAsync(userId, eventId);
        return NoContent();
    }

    /// <summary>
    /// Get logged in user's favorite events
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<FavoriteDto>>> GetMyFavorites()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var favorites = await _favoriteRepository.GetByUserIdAsync(userId);
        return Ok(favorites.Select(f => new FavoriteDto
        {
            EventId = f.EventId,
            EventTitle = f.Event?.Title ?? "Unknown Event",
            EventDescription = f.Event?.Description,
            EventDate = f.Event?.Date ?? DateTime.MinValue,
            EventLocation = f.Event?.Location,
            CategoryName = f.Event?.Category?.Name ?? "Unknown Category",
            AddedAt = f.AddedAt
        }));
    }
}

public class FavoriteDto
{
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public string? EventDescription { get; set; }
    public DateTime EventDate { get; set; }
    public string? EventLocation { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime AddedAt { get; set; }
}
