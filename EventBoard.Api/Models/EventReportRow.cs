namespace EventBoard.Api.Models;

/// <summary>
/// Read-only projection returned by the events reporting query.
/// Keyless: it is never persisted, only materialized from raw SQL.
/// Property names must match the column aliases in the reporting SQL.
/// </summary>
public class EventReportRow
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int TotalBookings { get; set; }
    public int ConfirmedBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int FavoritesCount { get; set; }
}
