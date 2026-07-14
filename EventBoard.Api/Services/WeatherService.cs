using System.Text.Json;
using EventBoard.Api.Models;

namespace EventBoard.Api.Services;

/// <summary>
/// Talks to the OpenWeatherMap "current weather" API. The HttpClient is configured
/// (base address + Polly retry) in Program.cs. All failures are swallowed and
/// surfaced as null so the endpoint can return a graceful "unavailable" response.
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<WeatherDto?> GetCurrentWeatherAsync(string city, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        var apiKey = _configuration["OpenWeather:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenWeather:ApiKey is not configured; weather is unavailable.");
            return null;
        }

        try
        {
            // Polly retry (configured on this HttpClient) handles transient failures.
            var requestUri = $"/data/2.5/weather?q={Uri.EscapeDataString(city)}&units=metric&appid={apiKey}";
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API returned {StatusCode} for city '{City}'", response.StatusCode, city);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var main = root.GetProperty("main");
            var weatherArray = root.GetProperty("weather");
            var firstWeather = weatherArray.EnumerateArray().FirstOrDefault();

            return new WeatherDto
            {
                Available = true,
                City = root.TryGetProperty("name", out var name) ? name.GetString() ?? city : city,
                TemperatureC = main.GetProperty("temp").GetDouble(),
                FeelsLikeC = main.TryGetProperty("feels_like", out var fl) ? fl.GetDouble() : 0,
                Humidity = main.TryGetProperty("humidity", out var h) ? h.GetInt32() : 0,
                Description = firstWeather.ValueKind == JsonValueKind.Object && firstWeather.TryGetProperty("description", out var d)
                    ? d.GetString() ?? string.Empty
                    : string.Empty,
                Icon = firstWeather.ValueKind == JsonValueKind.Object && firstWeather.TryGetProperty("icon", out var ic)
                    ? ic.GetString() ?? string.Empty
                    : string.Empty
            };
        }
        catch (Exception ex)
        {
            // Includes the case where Polly exhausts all retries.
            _logger.LogError(ex, "Failed to fetch weather for city '{City}'", city);
            return null;
        }
    }
}
