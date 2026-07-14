using EventBoard.Api.Models;

namespace EventBoard.Api.Services;

public interface IEventService
{
    Task<IEnumerable<Event>> GetAllEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
    Task<Event> CreateEventAsync(Event evt);
    Task<bool> DeleteEventAsync(int id);
}