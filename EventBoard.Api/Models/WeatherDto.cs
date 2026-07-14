namespace EventBoard.Api.Models;

/// <summary>
/// Current weather for an event's city. <see cref="Available"/> is false when the
/// upstream weather API could not be reached or returned no usable data.
/// </summary>
public class WeatherDto
{
    public bool Available { get; set; }
    public string City { get; set; } = string.Empty;
    public double TemperatureC { get; set; }
    public double FeelsLikeC { get; set; }
    public int Humidity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    /// <summary>A short upcoming forecast (a few daily snapshots). May be empty.</summary>
    public List<WeatherForecastItem> Forecast { get; set; } = new();
}

/// <summary>One point in the short forecast.</summary>
public class WeatherForecastItem
{
    public DateTime DateTime { get; set; }
    public double TemperatureC { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
