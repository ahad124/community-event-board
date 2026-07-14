using EventBoard.Api.Data;
using EventBoard.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EventBoard.Api.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EventBooking> CreateAsync(EventBooking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<EventBooking?> GetByIdAsync(int id)
    {
        return await _context.Bookings
            .Include(b => b.Event)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<EventBooking>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .Include(b => b.Event)
                .ThenInclude(e => e!.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<EventBooking>> GetByEventIdAsync(int eventId)
    {
        return await _context.Bookings
            .Where(b => b.EventId == eventId)
            .Include(b => b.User)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<EventBooking>> GetAllAsync()
    {
        return await _context.Bookings
            .Include(b => b.Event)
                .ThenInclude(e => e!.Category)
            .Include(b => b.User)
            .OrderByDescending(b => b.BookingDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateAsync(EventBooking booking)
    {
        _context.Entry(booking).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasBookingAsync(Guid userId, int eventId)
    {
        return await _context.Bookings
            .AnyAsync(b => b.UserId == userId && b.EventId == eventId);
    }

    public async Task<EventBooking?> GetByUserAndEventAsync(Guid userId, int eventId)
    {
        return await _context.Bookings
            .Include(b => b.Event)
                .ThenInclude(e => e!.Category)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.UserId == userId && b.EventId == eventId);
    }
}
