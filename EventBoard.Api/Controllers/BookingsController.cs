using System.Security.Claims;
using EventBoard.Api.Models;
using EventBoard.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(
        IBookingRepository bookingRepository,
        IEventRepository eventRepository,
        ILogger<BookingsController> logger)
    {
        _bookingRepository = bookingRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <summary>
    /// RSVP to an event (Authenticated Users). Accepts "Yes", "Maybe" or "No".
    /// If the user has already RSVP'd, their response is updated (upsert).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDto>> Rsvp([FromBody] CreateBookingRequest request)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized("Invalid user token claims.");
        }

        _logger.LogInformation("User {UserId} RSVPing '{Status}' to event {EventId}",
            userId, request.Status, request.EventId);

        var @event = await _eventRepository.GetByIdAsync(request.EventId);
        if (@event == null)
        {
            return NotFound($"Event with ID {request.EventId} not found.");
        }

        // Upsert: update the existing RSVP if there is one, otherwise create it.
        var existing = await _bookingRepository.GetByUserAndEventAsync(userId, request.EventId);
        if (existing != null)
        {
            existing.Status = request.Status;
            existing.BookingDate = DateTime.UtcNow;
            await _bookingRepository.UpdateAsync(existing);
            return Ok(MapToBookingDto(existing));
        }

        var booking = new EventBooking
        {
            EventId = request.EventId,
            UserId = userId,
            BookingDate = DateTime.UtcNow,
            Status = request.Status
        };

        var created = await _bookingRepository.CreateAsync(booking);
        var populated = await _bookingRepository.GetByIdAsync(created.Id);

        return CreatedAtAction(nameof(GetBookingById), new { id = created.Id }, MapToBookingDto(populated ?? created));
    }

    /// <summary>
    /// Get booking by ID (Must be owner or Admin)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BookingDto>> GetBookingById(int id)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (booking.UserId.ToString() != userIdString && userRole != "Admin")
        {
            return Forbid();
        }

        return Ok(MapToBookingDto(booking));
    }

    /// <summary>
    /// Get logged in user's bookings
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetMyBookings()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            return Unauthorized();
        }

        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        return Ok(bookings.Select(b => MapToBookingDto(b)));
    }

    /// <summary>
    /// Get bookings for a specific event (Admin only)
    /// </summary>
    [HttpGet("event/{eventId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetEventBookings(int eventId)
    {
        var bookings = await _bookingRepository.GetByEventIdAsync(eventId);
        return Ok(bookings.Select(b => MapToBookingDto(b)));
    }

    /// <summary>
    /// Get all bookings across all events (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<BookingDto>>> GetAllBookings()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        return Ok(bookings.Select(b => MapToBookingDto(b)));
    }

    /// <summary>
    /// Update booking status (Admin only)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateBookingStatusRequest request)
    {
        var booking = await _bookingRepository.GetByIdAsync(id);
        if (booking == null)
        {
            return NotFound();
        }

        booking.Status = request.Status;
        await _bookingRepository.UpdateAsync(booking);

        _logger.LogInformation("Booking {BookingId} status updated to {Status}", id, request.Status);
        return NoContent();
    }

    private static BookingDto MapToBookingDto(EventBooking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            EventId = booking.EventId,
            EventTitle = booking.Event?.Title ?? "Unknown Event",
            EventDate = booking.Event?.Date ?? DateTime.MinValue,
            EventLocation = booking.Event?.Location,
            CategoryName = booking.Event?.Category?.Name ?? "Unknown Category",
            UserId = booking.UserId,
            UserEmail = booking.User?.Email ?? "Unknown User",
            BookingDate = booking.BookingDate,
            Status = booking.Status.ToString()
        };
    }
}

public class CreateBookingRequest
{
    public int EventId { get; set; }

    /// <summary>RSVP response: "Yes", "Maybe" or "No". Defaults to "Yes".</summary>
    public BookingStatus Status { get; set; } = BookingStatus.Yes;
}

public class UpdateBookingStatusRequest
{
    public BookingStatus Status { get; set; }
}

public class BookingDto
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string? EventLocation { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
