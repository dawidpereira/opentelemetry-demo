using Microsoft.AspNetCore.Mvc;
using OpenTelemetryDemo.Api.Services;
using System.Diagnostics;

namespace OpenTelemetryDemo.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly TelemetryService _telemetry;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, TelemetryService telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Gets weather forecast for specified number of days
    /// </summary>
    /// <param name="days">Number of days to forecast (1-30)</param>
    /// <returns>Array of weather forecasts</returns>
    /// <response code="200">Returns the weather forecast</response>
    /// <response code="400">If days parameter is invalid</response>
    [HttpGet(Name = "GetWeatherForecast")]
    [ProducesResponseType(typeof(IEnumerable<WeatherForecast>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IEnumerable<WeatherForecast> Get([FromQuery] int days = 5)
    {
        using var activity = _telemetry.ActivitySource.StartActivity("GetWeatherForecast");
        activity?.SetTag("forecast.days", days);
        _logger.LogInformation("Generating weather forecast for {Days} days", days);
        _telemetry.RequestCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", "get_forecast"),
            new KeyValuePair<string, object?>("days", days.ToString()));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (days < 1 || days > 30)
            {
                _logger.LogWarning("Invalid number of days requested: {Days}", days);
                activity?.SetStatus(ActivityStatusCode.Error, "Invalid number of days");
                throw new ArgumentException("Days must be between 1 and 30");
            }

            var forecasts = Enumerable.Range(1, days).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            _logger.LogInformation("Successfully generated {Count} weather forecasts", forecasts.Length);
            activity?.SetTag("forecast.count", forecasts.Length);
            return forecasts;
        }
        finally
        {
            stopwatch.Stop();
            _telemetry.RequestDuration.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("endpoint", "get_forecast"));
        }
    }

    /// <summary>
    /// Gets weather forecast for a specific date
    /// </summary>
    /// <param name="date">Date to get forecast for (must be future date)</param>
    /// <returns>Weather forecast for the specified date</returns>
    /// <response code="200">Returns the weather forecast</response>
    /// <response code="400">If date is in the past</response>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(WeatherForecast), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WeatherForecast>> GetByDate(DateOnly date)
    {
        using var activity = _telemetry.ActivitySource.StartActivity("GetWeatherByDate");
        activity?.SetTag("forecast.date", date.ToString());

        _logger.LogInformation("Getting weather forecast for date: {Date}", date);
        _telemetry.RequestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "get_by_date"));

        await Task.Delay(Random.Shared.Next(10, 100));

        if (date < DateOnly.FromDateTime(DateTime.Now))
        {
            _logger.LogWarning("Requested date {Date} is in the past", date);
            return BadRequest("Cannot get forecast for past dates");
        }

        var forecast = new WeatherForecast
        {
            Date = date,
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        };

        _logger.LogInformation("Retrieved forecast for {Date}: {Temperature}°C, {Summary}",
            date, forecast.TemperatureC, forecast.Summary);

        return Ok(forecast);
    }

    /// <summary>
    /// Triggers an error for testing error handling and observability
    /// </summary>
    /// <returns>Always throws an exception</returns>
    /// <response code="500">Always returns internal server error</response>
    [HttpGet("error")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult SimulateError()
    {
        using var activity = _telemetry.ActivitySource.StartActivity("SimulateError");
        _logger.LogWarning("Simulating error endpoint called");
        _telemetry.RequestCounter.Add(1,
            new KeyValuePair<string, object?>("endpoint", "error"),
            new KeyValuePair<string, object?>("error", "true"));
        _telemetry.ErrorCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "error"));

        try
        {
            throw new InvalidOperationException("This is a simulated error for testing observability");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Simulated error occurred");
            activity?.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, new ActivityTagsCollection { ["exception.message"] = ex.Message, ["exception.type"] = ex.GetType().FullName }));
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Creates an artificial delay for testing performance monitoring
    /// </summary>
    /// <param name="delayMs">Delay in milliseconds</param>
    /// <returns>Response after specified delay</returns>
    /// <response code="200">Returns success message after delay</response>
    [HttpGet("slow")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SlowEndpoint([FromQuery] int delayMs = 2000)
    {
        using var activity = _telemetry.ActivitySource.StartActivity("SlowEndpoint");
        activity?.SetTag("delay.ms", delayMs);
        _logger.LogInformation("Slow endpoint called with delay: {DelayMs}ms", delayMs);
        _telemetry.RequestCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "slow"));

        using (var childActivity = _telemetry.ActivitySource.StartActivity("ProcessingDelay"))
        {
            childActivity?.SetTag("processing.type", "artificial_delay");
            await Task.Delay(delayMs);
        }

        _logger.LogInformation("Slow endpoint completed after {DelayMs}ms", delayMs);
        return Ok(new { message = $"Response after {delayMs}ms delay", timestamp = DateTime.UtcNow });
    }
}
