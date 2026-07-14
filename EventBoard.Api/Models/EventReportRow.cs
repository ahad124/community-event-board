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
    public int TotalRsvps { get; set; }
    public int YesRsvps { get; set; }
    public int MaybeRsvps { get; set; }
    public int NoRsvps { get; set; }
    public int FavoritesCount { get; set; }
}
