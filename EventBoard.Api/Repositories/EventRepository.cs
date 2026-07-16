using EventBoard.Api.Data;
using EventBoard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventBoard.Api.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Event>> GetAllAsync()
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Bookings)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await _context.Events
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Bookings)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Event>> GetByCategoryIdAsync(int categoryId)
    {
        return await _context.Events
            .Where(e => e.CategoryId == categoryId)
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> GetByOrganizerIdAsync(Guid organizerId)
    {
        return await _context.Events
            .Where(e => e.OrganizerId == organizerId)
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .Include(e => e.Bookings)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Event>> SearchByTitleAsync(string term)
    {
        // Title search via EF Core LINQ. The term is bound as a parameter by the
        // provider (translated to a parameterized LIKE), so it can never alter the
        // SQL structure — safe from SQL injection. EF also escapes LIKE wildcards.
        term = term.Trim();

        return await _context.Events
            .Where(e => e.Title.Contains(term))
            .Include(e => e.Category)
            .Include(e => e.Organizer)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<EventDetailedDto>> GetDetailedAsync(
        int? categoryId, DateTime? from, DateTime? to, string? location, string? q)
    {
        // Load every event, then filter in memory (does not use the DB indexes).
        var all = await _context.Events.ToListAsync();

        var filtered = all.Where(e =>
            (!categoryId.HasValue || e.CategoryId == categoryId.Value) &&
            (!from.HasValue || e.Date >= from.Value) &&
            (!to.HasValue || e.Date <= to.Value) &&
            (string.IsNullOrWhiteSpace(location) ||
                (e.Location ?? string.Empty).Contains(location, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(q) ||
                e.Title.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var result = new List<EventDetailedDto>(filtered.Count);

        // For each event, fetch its related data one query at a time (N+1).
        foreach (var e in filtered)
        {
            var category = await _context.Categories.FindAsync(e.CategoryId);
            var organizer = await _context.Users.FindAsync(e.OrganizerId);
            var bookings = await _context.Bookings
                .Where(b => b.EventId == e.Id)
                .ToListAsync();
            var favoritesCount = await _context.Favorites
                .Where(f => f.EventId == e.Id)
                .CountAsync();

            result.Add(new EventDetailedDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Date = e.Date,
                Location = e.Location,
                ImageUrl = e.ImageUrl,
                CategoryId = e.CategoryId,
                CategoryName = category?.Name ?? "Unknown Category",
                OrganizerId = e.OrganizerId,
                OrganizerEmail = organizer?.Email ?? "Unknown",
                RsvpYesCount = bookings.Count(b => b.Status == BookingStatus.Yes),
                RsvpMaybeCount = bookings.Count(b => b.Status == BookingStatus.Maybe),
                RsvpNoCount = bookings.Count(b => b.Status == BookingStatus.No),
                RsvpTotalCount = bookings.Count,
                FavoritesCount = favoritesCount
            });
        }

        return result;
    }

    public async Task<Event> CreateAsync(Event @event)
    {
        _context.Events.Add(@event);
        await _context.SaveChangesAsync();
        return @event;
    }

    public async Task UpdateAsync(Event @event)
    {
        _context.Entry(@event).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event != null)
        {
            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();
        }
    }
}
