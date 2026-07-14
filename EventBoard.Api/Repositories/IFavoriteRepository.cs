using EventBoard.Api.Models;

namespace EventBoard.Api.Repositories;

public interface IFavoriteRepository
{
    Task AddAsync(EventFavorite favorite);
    Task RemoveAsync(Guid userId, int eventId);
    Task<IEnumerable<EventFavorite>> GetByUserIdAsync(Guid userId);
    Task<bool> ExistsAsync(Guid userId, int eventId);
}
