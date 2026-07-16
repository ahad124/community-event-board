using EventBoard.Api.Models;

namespace EventBoard.Api.Repositories;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<Event?> GetByIdAsync(int id);
    Task<IEnumerable<Event>> GetByCategoryIdAsync(int categoryId);
    Task<IEnumerable<Event>> GetByOrganizerIdAsync(Guid organizerId);
    Task<IEnumerable<Event>> SearchByTitleAsync(string term);
    Task<IEnumerable<EventDetailedDto>> GetDetailedAsync(
        int? categoryId, DateTime? from, DateTime? to, string? location, string? q);
    Task<Event> CreateAsync(Event @event);
    Task UpdateAsync(Event @event);
    Task DeleteAsync(int id);
}
