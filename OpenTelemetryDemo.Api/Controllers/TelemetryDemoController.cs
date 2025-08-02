using Microsoft.AspNetCore.Mvc;
using OpenTelemetryDemo.Api.Services;
using System.Diagnostics;

namespace OpenTelemetryDemo.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class TelemetryDemoController : ControllerBase
{
    private readonly ILogger<TelemetryDemoController> _logger;
    private readonly TelemetryService _telemetry;

    public TelemetryDemoController(ILogger<TelemetryDemoController> logger, TelemetryService telemetry)
    {
        _logger = logger;
        _telemetry = telemetry;
    }

    /// <summary>
    /// Demonstrates baggage propagation across spans
    /// </summary>
    /// <param name="userId">User ID to propagate as baggage</param>
    /// <param name="tenantId">Tenant ID to propagate as baggage</param>
    /// <returns>Baggage propagation example result</returns>
    [HttpGet("baggage")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DemonstrateBaggage([FromQuery] string userId = "user123", [FromQuery] string tenantId = "tenant456")
    {
        Activity.Current?.SetBaggage("user.id", userId);
        Activity.Current?.SetBaggage("tenant.id", tenantId);

        using var activity = _telemetry.ActivitySource.StartActivity("BaggageDemo");
        activity?.SetTag("demo.type", "baggage");

        _logger.LogInformation("Starting baggage propagation demo for user {UserId} in tenant {TenantId}", userId, tenantId);

        var result1 = await ProcessWithBaggage("Service1");
        var result2 = await ProcessWithBaggage("Service2");

        var currentBaggage = Activity.Current?.Baggage?
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string?>();

        return Ok(new
        {
            message = "Baggage propagation demo completed",
            baggage = currentBaggage,
            service1Result = result1,
            service2Result = result2
        });
    }

    private async Task<object> ProcessWithBaggage(string serviceName)
    {
        using var activity = _telemetry.ActivitySource.StartActivity($"Process{serviceName}");

        var userId = Activity.Current?.GetBaggageItem("user.id");
        var tenantId = Activity.Current?.GetBaggageItem("tenant.id");

        activity?.SetTag("service.name", serviceName);
        activity?.SetTag("has.user.id", !string.IsNullOrEmpty(userId));
        activity?.SetTag("has.tenant.id", !string.IsNullOrEmpty(tenantId));

        _logger.LogInformation("{ServiceName} processing with user {UserId} and tenant {TenantId}",
            serviceName, userId, tenantId);

        await Task.Delay(Random.Shared.Next(50, 150));

        return new
        {
            service = serviceName,
            processedAt = DateTime.UtcNow,
            userId,
            tenantId
        };
    }

    /// <summary>
    /// Demonstrates span links for batch processing
    /// </summary>
    /// <param name="itemCount">Number of items to process in batch</param>
    /// <returns>Batch processing result with linked spans</returns>
    [HttpGet("span-links")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DemonstrateSpanLinks([FromQuery] int itemCount = 5)
    {
        _telemetry.IncrementActiveRequests();

        try
        {
            using var batchActivity = _telemetry.ActivitySource.StartActivity("BatchProcessingWithLinks");
            batchActivity?.SetTag("batch.size", itemCount);

            var itemSpans = new List<ActivityContext>();
            var results = new List<object>();

            for (int i = 0; i < itemCount; i++)
            {
                using var itemActivity = _telemetry.ActivitySource.StartActivity($"ProcessItem_{i}");
                if (itemActivity != null)
                {
                    itemActivity.SetTag("item.id", i);
                    itemSpans.Add(itemActivity.Context);

                    await Task.Delay(Random.Shared.Next(10, 50));
                    results.Add(new { itemId = i, processedAt = DateTime.UtcNow });
                }
            }

            var links = itemSpans.Select(ctx => new ActivityLink(ctx)).ToArray();
            using var summaryActivity = _telemetry.ActivitySource.StartActivity(
                "BatchSummary",
                ActivityKind.Internal,
                parentContext: default,
                links: links);

            summaryActivity?.SetTag("linked.spans.count", itemSpans.Count);
            summaryActivity?.AddEvent(new ActivityEvent("BatchCompleted",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection { ["item.count"] = itemCount }));

            _telemetry.RequestCounter.Add(1,
                new KeyValuePair<string, object?>("operation", "batch_with_links"));

            return Ok(new
            {
                message = "Span links demonstration completed",
                itemsProcessed = itemCount,
                linkedSpans = itemSpans.Count,
                results
            });
        }
        finally
        {
            _telemetry.DecrementActiveRequests();
        }
    }

    /// <summary>
    /// Demonstrates various metric types including UpDownCounter
    /// </summary>
    /// <returns>Current metric values</returns>
    [HttpGet("metrics-demo")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DemonstrateMetrics()
    {
        using var activity = _telemetry.ActivitySource.StartActivity("MetricsDemo");

        _telemetry.UpdateQueueDepth(Random.Shared.Next(1, 5));
        await Task.Delay(100);
        _telemetry.UpdateQueueDepth(-Random.Shared.Next(1, 3));

        _telemetry.RequestDuration.Record(Random.Shared.Next(50, 500));
        _telemetry.ProcessingTime.Record(Random.Shared.Next(10, 100));

        return Ok(new
        {
            message = "Metrics demonstration completed",
            activeRequests = _telemetry.GetActiveRequestCount(),
            queueDepth = _telemetry.GetQueueDepth(),
            metricTypes = new
            {
                counters = new[] { "RequestCounter", "ErrorCounter" },
                histograms = new[] { "RequestDuration", "ProcessingTime" },
                upDownCounters = new[] { "ActiveRequests", "QueueDepth" },
                observableGauges = new[] { "CpuUsage" },
                observableUpDownCounters = new[] { "ThreadPoolSize" }
            }
        });
    }

    /// <summary>
    /// Demonstrates custom span attributes and events
    /// </summary>
    /// <param name="complexity">Processing complexity (1-10)</param>
    /// <returns>Processing result with custom telemetry</returns>
    [HttpGet("custom-telemetry")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DemonstrateCustomTelemetry([FromQuery] int complexity = 5)
    {
        using var activity = _telemetry.ActivitySource.StartActivity("CustomTelemetryDemo", ActivityKind.Server);

        activity?.SetTag("app.feature", "telemetry-demo");
        activity?.SetTag("app.version", TelemetryService.ServiceVersion);
        activity?.SetTag("processing.complexity", complexity);
        activity?.SetTag("processing.algorithm", "recursive");

        activity?.AddEvent(new ActivityEvent("ProcessingStarted",
            DateTimeOffset.UtcNow,
            new ActivityTagsCollection
            {
                ["complexity"] = complexity,
                ["expected.duration.ms"] = complexity * 100
            }));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            for (int stage = 1; stage <= complexity; stage++)
            {
                activity?.AddEvent(new ActivityEvent($"Stage{stage}Started"));

                await Task.Delay(Random.Shared.Next(50, 150));

                activity?.AddEvent(new ActivityEvent($"Stage{stage}Completed",
                    DateTimeOffset.UtcNow,
                    new ActivityTagsCollection { ["duration.ms"] = stopwatch.ElapsedMilliseconds }));
            }

            stopwatch.Stop();

            _telemetry.RequestCounter.Add(1,
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("complexity", complexity));

            _telemetry.ProcessingTime.Record(stopwatch.ElapsedMilliseconds,
                new KeyValuePair<string, object?>("complexity", complexity));

            activity?.SetStatus(ActivityStatusCode.Ok, "Processing completed successfully");

            return Ok(new
            {
                message = "Custom telemetry demonstration completed",
                complexity,
                stages = complexity,
                totalDurationMs = stopwatch.ElapsedMilliseconds,
                averageStageMs = stopwatch.ElapsedMilliseconds / complexity
            });
        }
        catch (Exception ex)
        {
            _telemetry.ErrorCounter.Add(1,
                new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddEvent(new ActivityEvent("Exception",
                DateTimeOffset.UtcNow,
                new ActivityTagsCollection
                {
                    ["exception.type"] = ex.GetType().FullName,
                    ["exception.message"] = ex.Message
                }));

            throw;
        }
    }

    /// <summary>
    /// Demonstrates manual context propagation
    /// </summary>
    /// <returns>Context propagation information</returns>
    [HttpGet("context-propagation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DemonstrateContextPropagation()
    {
        using var parentActivity = _telemetry.ActivitySource.StartActivity("ParentOperation");
        var parentContext = Activity.Current?.Context ?? default;

        var result = await Task.Run(async () =>
        {
            using var childActivity = _telemetry.ActivitySource.StartActivity(
                "ChildOperation",
                ActivityKind.Internal,
                parentContext);

            childActivity?.SetTag("manually.propagated", true);

            await Task.Delay(100);

            return new
            {
                parentTraceId = parentContext.TraceId.ToString(),
                parentSpanId = parentContext.SpanId.ToString(),
                childTraceId = childActivity?.TraceId.ToString(),
                childSpanId = childActivity?.SpanId.ToString(),
                isSameTrace = childActivity?.TraceId == parentContext.TraceId
            };
        });

        return Ok(new
        {
            message = "Context propagation demonstration completed",
            result
        });
    }
}

