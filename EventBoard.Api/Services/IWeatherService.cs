using EventBoard.Api.Models;

namespace EventBoard.Api.Services;

public interface IWeatherService
{
    /// <summary>
    /// Fetches current weather for the given city. Returns null when the city is
    /// empty or the upstream API cannot provide data (caller shows a fallback).
    /// </summary>
    Task<WeatherDto?> GetCurrentWeatherAsync(string city, CancellationToken cancellationToken = default);
}
