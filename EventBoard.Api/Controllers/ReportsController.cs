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
    /// Per-event booking report (Admin only). For each event it returns the
    /// category, total bookings, a breakdown by booking status, and the number
    /// of times the event has been favorited. Optionally filtered by event date.
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
    COUNT(b.Id)                                                AS TotalBookings,
    SUM(CASE WHEN b.Status = 'Confirmed' THEN 1 ELSE 0 END)    AS ConfirmedBookings,
    SUM(CASE WHEN b.Status = 'Pending'   THEN 1 ELSE 0 END)    AS PendingBookings,
    SUM(CASE WHEN b.Status = 'Cancelled' THEN 1 ELSE 0 END)    AS CancelledBookings,
    (SELECT COUNT(*) FROM Favorites f WHERE f.EventId = e.Id)  AS FavoritesCount
FROM Events e
INNER JOIN Categories c ON c.Id = e.CategoryId
LEFT JOIN Bookings b ON b.EventId = e.Id
WHERE (@fromDate IS NULL OR e.Date >= @fromDate)
  AND (@toDate   IS NULL OR e.Date <= @toDate)
GROUP BY e.Id, e.Title, c.Name, e.Date
ORDER BY TotalBookings DESC, e.Date ASC;";

        var fromParam = new SqlParameter("@fromDate", (object?)from ?? DBNull.Value);
        var toParam = new SqlParameter("@toDate", (object?)to ?? DBNull.Value);

        var report = await _context.EventReport
            .FromSqlRaw(sql, fromParam, toParam)
            .AsNoTracking()
            .ToListAsync();

        return Ok(report);
    }
}
