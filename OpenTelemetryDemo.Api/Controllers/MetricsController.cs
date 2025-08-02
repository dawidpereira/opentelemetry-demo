using Microsoft.AspNetCore.Mvc;
using OpenTelemetryDemo.Api.Services;
using System.Diagnostics;

namespace OpenTelemetryDemo.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class MetricsController : ControllerBase
{
    private readonly ILogger<MetricsController> _logger;
    private readonly TelemetryService _telemetry;

    public MetricsController(ILogger<MetricsController> logger, TelemetryService telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Processes a request with configurable complexity for metrics testing
    /// </summary>
    /// <param name="complexity">Processing complexity level (1-10)</param>
    /// <returns>Processing result with timing information</returns>
    /// <response code="200">Returns processing result</response>
    /// <response code="500">If processing fails</response>
    [HttpGet("process")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ProcessRequest([FromQuery] int complexity = 1)
    {
        _telemetry.IncrementActiveRequests();

        using var activity = _telemetry.ActivitySource.StartActivity("ProcessRequest");
        activity?.SetTag("complexity", complexity);

        _logger.LogInformation("Processing request with complexity: {Complexity}", complexity);
        _telemetry.RequestCounter.Add(1,
            new KeyValuePair<string, object?>("operation", "process"),
            new KeyValuePair<string, object?>("complexity", complexity.ToString()));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using (var span = _telemetry.ActivitySource.StartActivity("ComputeResult"))
            {
                span?.SetTag("computation.type", "fibonacci");

                var result = await Task.Run(() => ComputeFibonacci(Math.Min(complexity * 10, 40)));

                span?.SetTag("computation.result", result);
                _logger.LogDebug("Computed Fibonacci({N}) = {Result}", complexity * 10, result);
            }

            await Task.Delay(complexity * 100);

            stopwatch.Stop();
            _telemetry.ProcessingTime.Record(stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("complexity", complexity.ToString()));

            return Ok(new
            {
                complexity,
                processingTimeMs = stopwatch.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request with complexity {Complexity}", complexity);
            activity?.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow, new ActivityTagsCollection { ["exception.message"] = ex.Message, ["exception.type"] = ex.GetType().FullName }));
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            return StatusCode(500, "Processing error");
        }
        finally
        {
            _telemetry.DecrementActiveRequests();
        }
    }

    /// <summary>
    /// Processes multiple items in batch for testing concurrent operations
    /// </summary>
    /// <param name="count">Number of items to process</param>
    /// <returns>Batch processing results</returns>
    /// <response code="200">Returns batch results</response>
    [HttpGet("batch")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> BatchProcess([FromQuery] int count = 5)
    {
        using var activity = _telemetry.ActivitySource.StartActivity("BatchProcess");
        activity?.SetTag("batch.size", count);

        _logger.LogInformation("Starting batch process with {Count} items", count);
        _telemetry.RequestCounter.Add(1, new KeyValuePair<string, object?>("operation", "batch"));

        var tasks = new List<Task<int>>();

        for (int i = 0; i < count; i++)
        {
            var index = i;
            tasks.Add(ProcessItem(index));
        }

        var results = await Task.WhenAll(tasks);

        _logger.LogInformation("Batch process completed. Processed {Count} items", results.Length);
        activity?.SetTag("batch.completed", results.Length);

        return Ok(new
        {
            processed = results.Length,
            totalSum = results.Sum(),
            results
        });
    }

    private async Task<int> ProcessItem(int index)
    {
        using var activity = _telemetry.ActivitySource.StartActivity("ProcessItem");
        activity?.SetTag("item.index", index);

        await Task.Delay(Random.Shared.Next(50, 200));

        var result = Random.Shared.Next(1, 100);
        _logger.LogDebug("Processed item {Index} with result {Result}", index, result);

        return result;
    }

    private long ComputeFibonacci(int n)
    {
        if (n <= 1) return n;
        return ComputeFibonacci(n - 1) + ComputeFibonacci(n - 2);
    }

    /// <summary>
    /// Gets the current status of the metrics service
    /// </summary>
    /// <returns>Service status information</returns>
    /// <response code="200">Returns service status</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        _logger.LogInformation("Status endpoint called");

        return Ok(new
        {
            status = "healthy",
            activeRequests = _telemetry.GetActiveRequestCount(),
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
        });
    }
}
