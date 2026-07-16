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
        // Single, set-based query. Filters are applied in the database (using the
        // Date/CategoryId/Location indexes) and related data is fetched in one round
        // trip via a projection — the per-event aggregates (RSVP tallies, favorites)
        // are computed by the database, not N+1 round trips.
        var query = _context.Events.AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }
        if (from.HasValue)
        {
            query = query.Where(e => e.Date >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(e => e.Date <= to.Value);
        }
        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(e => e.Location != null && e.Location.Contains(location));
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(e => e.Title.Contains(q));
        }

        return await query
            .Select(e => new EventDetailedDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                Date = e.Date,
                Location = e.Location,
                ImageUrl = e.ImageUrl,
                CategoryId = e.CategoryId,
                CategoryName = e.Category!.Name,
                OrganizerId = e.OrganizerId,
                OrganizerEmail = e.Organizer!.Email,
                RsvpYesCount = e.Bookings.Count(b => b.Status == BookingStatus.Yes),
                RsvpMaybeCount = e.Bookings.Count(b => b.Status == BookingStatus.Maybe),
                RsvpNoCount = e.Bookings.Count(b => b.Status == BookingStatus.No),
                RsvpTotalCount = e.Bookings.Count,
                FavoritesCount = e.Favorites.Count
            })
            .ToListAsync();
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
