using EventBoard.Api.Models;
using EventBoard.Api.Repositories;

namespace EventBoard.Api.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _repository;

    public EventService(IEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<Event> CreateEventAsync(Event evt)
    {
        if (evt == null)
            throw new ArgumentNullException(nameof(evt));

        return await _repository.CreateAsync(evt);
    }

    public async Task<IEnumerable<Event>> GetAllEventsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        if (id <= 0)
            return null;

        return await _repository.GetByIdAsync(id);
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        if (id <= 0)
            return false;

        var existing = await _repository.GetByIdAsync(id);

        if (existing == null)
            return false;

        await _repository.DeleteAsync(id);

        return true;
    }
}