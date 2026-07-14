namespace EventBoard.Api.Models;

/// <summary>
/// A user's RSVP response to an event.
/// Persisted as its string name (see AppDbContext) so reports read naturally.
/// </summary>
public enum BookingStatus
{
    Yes,
    Maybe,
    No
}
