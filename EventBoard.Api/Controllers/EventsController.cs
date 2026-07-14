using System.Security.Claims;
using EventBoard.Api.Models;
using EventBoard.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventRepository _eventRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EventsController> _logger;

    // Whitelisted image types for upload. Extension AND content-type must both match.
    private static readonly HashSet<string> AllowedImageExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly HashSet<string> AllowedImageContentTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024; // 5 MB

    public EventsController(
        IEventRepository eventRepository,
        IWebHostEnvironment environment,
        ILogger<EventsController> logger)
    {
        _eventRepository = eventRepository;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Get all events
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAllEvents()
    {
        _logger.LogInformation("Retrieving all events");
        var events = await _eventRepository.GetAllAsync();
        return Ok(events.Select(e => MapToEventDto(e)));
    }

    /// <summary>
    /// Get event by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEventById(int id)
    {
        _logger.LogInformation("Retrieving event with ID: {EventId}", id);

        if (id <= 0)
        {
            return BadRequest("Event ID must be greater than 0");
        }

        var evt = await _eventRepository.GetByIdAsync(id);

        if (evt == null)
        {
            _logger.LogWarning("Event not found with ID: {EventId}", id);
            return NotFound($"Event with ID {id} not found");
        }

        return Ok(MapToEventDto(evt));
    }

    /// <summary>
    /// Get events created by the currently authenticated user.
    /// </summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetMyEvents()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var organizerId))
        {
            return Unauthorized("Invalid user token claims.");
        }

        var events = await _eventRepository.GetByOrganizerIdAsync(organizerId);
        return Ok(events.Select(e => MapToEventDto(e)));
    }

    /// <summary>
    /// Get events by category ID
    /// </summary>
    [HttpGet("category/{categoryId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEventsByCategoryId(int categoryId)
    {
        _logger.LogInformation("Retrieving events for category ID: {CategoryId}", categoryId);
        var events = await _eventRepository.GetByCategoryIdAsync(categoryId);
        return Ok(events.Select(e => MapToEventDto(e)));
    }

    /// <summary>
    /// Create a new event (any authenticated user). The organizer is taken from
    /// the JWT, so callers cannot create events on behalf of someone else.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
    {
        _logger.LogInformation("Creating new event: {Title}", request.Title);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var organizerId))
        {
            return Unauthorized("Invalid user token claims.");
        }

        var evt = new Event
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Date = request.Date,
            Location = request.Location?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            CategoryId = request.CategoryId,
            OrganizerId = organizerId
        };

        var created = await _eventRepository.CreateAsync(evt);
        // Re-fetch to get Category and Organizer names populated
        var populated = await _eventRepository.GetByIdAsync(created.Id);

        _logger.LogInformation("Event created successfully with ID: {EventId}", created.Id);
        return CreatedAtAction(nameof(GetEventById), new { id = created.Id }, MapToEventDto(populated ?? created));
    }

    /// <summary>
    /// Update an existing event (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        _logger.LogInformation("Updating event with ID: {EventId}", id);

        if (id <= 0)
        {
            return BadRequest("Event ID must be greater than 0");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var evt = await _eventRepository.GetByIdAsync(id);

        if (evt == null)
        {
            _logger.LogWarning("Event not found with ID: {EventId}", id);
            return NotFound($"Event with ID {id} not found");
        }

        evt.Title = request.Title?.Trim() ?? evt.Title;
        evt.Description = request.Description?.Trim() ?? evt.Description;
        evt.Date = request.Date ?? evt.Date;
        evt.Location = request.Location?.Trim() ?? evt.Location;
        evt.ImageUrl = request.ImageUrl?.Trim() ?? evt.ImageUrl;
        evt.CategoryId = request.CategoryId ?? evt.CategoryId;

        await _eventRepository.UpdateAsync(evt);

        // Re-fetch populated
        var populated = await _eventRepository.GetByIdAsync(id);

        _logger.LogInformation("Event updated successfully with ID: {EventId}", id);
        return Ok(MapToEventDto(populated ?? evt));
    }

    /// <summary>
    /// Delete an event (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        _logger.LogInformation("Deleting event with ID: {EventId}", id);

        if (id <= 0)
        {
            return BadRequest("Event ID must be greater than 0");
        }

        var evt = await _eventRepository.GetByIdAsync(id);
        if (evt == null)
        {
            _logger.LogWarning("Event not found with ID: {EventId}", id);
            return NotFound($"Event with ID {id} not found");
        }

        await _eventRepository.DeleteAsync(id);
        _logger.LogInformation("Event deleted successfully with ID: {EventId}", id);
        return NoContent();
    }

    /// <summary>
    /// Upload an event image (any authenticated user). Returns the relative URL to
    /// store on the event. Validates content-type, extension and size, and writes with
    /// a random file name to prevent path traversal or overwriting existing files.
    /// </summary>
    [HttpPost("upload-image")]
    [Authorize]
    [RequestSizeLimit(MaxImageBytes)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        if (file.Length > MaxImageBytes)
        {
            return BadRequest("Image must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
        {
            return BadRequest("Unsupported file type. Allowed: .jpg, .jpeg, .png, .gif, .webp");
        }

        if (!AllowedImageContentTypes.Contains(file.ContentType))
        {
            return BadRequest("Unsupported content type. Please upload a valid image.");
        }

        // wwwroot may not exist yet in a fresh checkout; ensure the uploads folder exists.
        var webRoot = _environment.WebRootPath
                      ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
        var uploadsDir = Path.Combine(webRoot, "uploads");
        Directory.CreateDirectory(uploadsDir);

        // Random, server-generated file name — never trust the client's file name.
        var safeFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(uploadsDir, safeFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{safeFileName}";
        _logger.LogInformation("Image uploaded: {ImageUrl}", relativeUrl);
        return Ok(new { imageUrl = relativeUrl });
    }

    private static EventDto MapToEventDto(Event evt)
    {
        var bookings = evt.Bookings ?? new List<EventBooking>();
        return new EventDto
        {
            Id = evt.Id,
            Title = evt.Title,
            Description = evt.Description,
            Date = evt.Date,
            Location = evt.Location,
            ImageUrl = evt.ImageUrl,
            CategoryId = evt.CategoryId,
            CategoryName = evt.Category?.Name ?? "Unknown Category",
            OrganizerId = evt.OrganizerId,
            OrganizerEmail = evt.Organizer?.Email ?? "Unknown",
            RsvpYesCount = bookings.Count(b => b.Status == BookingStatus.Yes),
            RsvpMaybeCount = bookings.Count(b => b.Status == BookingStatus.Maybe),
            RsvpNoCount = bookings.Count(b => b.Status == BookingStatus.No),
            RsvpTotalCount = bookings.Count
        };
    }
}

/// <summary>
/// DTO for creating an event
/// </summary>
public class CreateEventRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
}

/// <summary>
/// DTO for updating an event
/// </summary>
public class UpdateEventRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
}

/// <summary>
/// DTO for event response
/// </summary>
public class EventDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid OrganizerId { get; set; }
    public string OrganizerEmail { get; set; } = string.Empty;

    // RSVP tallies (shown on the event detail page)
    public int RsvpYesCount { get; set; }
    public int RsvpMaybeCount { get; set; }
    public int RsvpNoCount { get; set; }
    public int RsvpTotalCount { get; set; }
}
