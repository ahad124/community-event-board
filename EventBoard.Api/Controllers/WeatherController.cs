using EventBoard.Api.Models;
using EventBoard.Api.Repositories;
using EventBoard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventBoard.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<WeatherController> _logger;

    public WeatherController(
        IWeatherService weatherService,
        IEventRepository eventRepository,
        ILogger<WeatherController> logger)
    {
        _weatherService = weatherService;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    /// <summary>
    /// Current weather for a given event's city. Always returns 200 with a
    /// WeatherDto; when weather can't be fetched, Available is false and the
    /// client shows "Weather information is currently unavailable."
    /// </summary>
    [HttpGet("event/{eventId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WeatherDto>> GetWeatherForEvent(int eventId)
    {
        var evt = await _eventRepository.GetByIdAsync(eventId);
        if (evt == null)
        {
            return NotFound($"Event with ID {eventId} not found");
        }

        var city = ExtractCity(evt.Location);
        var weather = await _weatherService.GetCurrentWeatherAsync(city);

        // Never surface a hard error to the UI — an unavailable result is a valid state.
        return Ok(weather ?? new WeatherDto { Available = false, City = city });
    }

    /// <summary>
    /// Locations are free text (e.g. "San Francisco Convention Center, CA").
    /// Use the segment before the first comma as a best-effort city name.
    /// </summary>
    private static string ExtractCity(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return string.Empty;
        }

        var firstSegment = location.Split(',')[0].Trim();
        return firstSegment;
    }
}
