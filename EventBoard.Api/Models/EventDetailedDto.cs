namespace EventBoard.Api.Models;

/// <summary>
/// Rich per-event detail returned by the "detailed events" listing
/// (GET /api/events/detailed): organizer, category, RSVP tallies and favorites.
/// </summary>
public class EventDetailedDto
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

    public int RsvpYesCount { get; set; }
    public int RsvpMaybeCount { get; set; }
    public int RsvpNoCount { get; set; }
    public int RsvpTotalCount { get; set; }

    public int FavoritesCount { get; set; }
}
