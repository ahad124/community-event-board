using EventBoard.Api.Data;
using EventBoard.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EventBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReportsController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Per-event RSVP report (Admin only). For each event it returns the
    /// category, total RSVPs, a breakdown by RSVP response (Yes/Maybe/No), and the
    /// number of times the event has been favorited. Optionally filtered by event date.
    /// </summary>
    /// <param name="from">Only include events on or after this date (inclusive).</param>
    /// <param name="to">Only include events on or before this date (inclusive).</param>
    [HttpGet("events")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<EventReportRow>>> GetEventsReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        // NOTE: This raw SQL reporting query was generated with AI assistance.
        // It aggregates across Events, Categories, Bookings and Favorites in a
        // single round trip. Values are passed as parameters (never string
        // concatenation) so the query is safe from SQL injection.
        const string sql = @"
SELECT
    e.Id                                                       AS EventId,
    e.Title                                                    AS Title,
    c.Name                                                     AS CategoryName,
    e.Date                                                     AS EventDate,
    COUNT(b.Id)                                          AS TotalRsvps,
    SUM(CASE WHEN b.Status = 'Yes'   THEN 1 ELSE 0 END)  AS YesRsvps,
    SUM(CASE WHEN b.Status = 'Maybe' THEN 1 ELSE 0 END)  AS MaybeRsvps,
    SUM(CASE WHEN b.Status = 'No'    THEN 1 ELSE 0 END)  AS NoRsvps,
    (SELECT COUNT(*) FROM Favorites f WHERE f.EventId = e.Id)  AS FavoritesCount
FROM Events e
INNER JOIN Categories c ON c.Id = e.CategoryId
LEFT JOIN Bookings b ON b.EventId = e.Id
WHERE (@fromDate IS NULL OR e.Date >= @fromDate)
  AND (@toDate   IS NULL OR e.Date <= @toDate)
GROUP BY e.Id, e.Title, c.Name, e.Date
ORDER BY TotalRsvps DESC, e.Date ASC;";

        var fromParam = new SqlParameter("@fromDate", (object?)from ?? DBNull.Value);
        var toParam = new SqlParameter("@toDate", (object?)to ?? DBNull.Value);

        var report = await _context.EventReport
            .FromSqlRaw(sql, fromParam, toParam)
            .AsNoTracking()
            .ToListAsync();

        return Ok(report);
    }

    /// <summary>
    /// Summary counts for the admin dashboard (Admin only): total users, events,
    /// RSVPs (with a Yes/Maybe/No breakdown), categories and favorites.
    /// </summary>
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<StatsDto>> GetStats()
    {
        var stats = new StatsDto
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalEvents = await _context.Events.CountAsync(),
            TotalCategories = await _context.Categories.CountAsync(),
            TotalRsvps = await _context.Bookings.CountAsync(),
            YesRsvps = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Yes),
            MaybeRsvps = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Maybe),
            NoRsvps = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.No),
            TotalFavorites = await _context.Favorites.CountAsync()
        };

        return Ok(stats);
    }
}

public class StatsDto
{
    public int TotalUsers { get; set; }
    public int TotalEvents { get; set; }
    public int TotalCategories { get; set; }
    public int TotalRsvps { get; set; }
    public int YesRsvps { get; set; }
    public int MaybeRsvps { get; set; }
    public int NoRsvps { get; set; }
    public int TotalFavorites { get; set; }
}
