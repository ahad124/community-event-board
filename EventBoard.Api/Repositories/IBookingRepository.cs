using EventBoard.Api.Models;

namespace EventBoard.Api.Repositories;

public interface IBookingRepository
{
    Task<EventBooking> CreateAsync(EventBooking booking);
    Task<EventBooking?> GetByIdAsync(int id);
    Task<IEnumerable<EventBooking>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<EventBooking>> GetByEventIdAsync(int eventId);
    Task<IEnumerable<EventBooking>> GetAllAsync();
    Task UpdateAsync(EventBooking booking);
    Task<bool> HasBookingAsync(Guid userId, int eventId);
    Task<EventBooking?> GetByUserAndEventAsync(Guid userId, int eventId);
}
