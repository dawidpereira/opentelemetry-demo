# OpenTelemetry Demo - Comprehensive Feature Guide

This guide walks through all the OpenTelemetry features demonstrated in this project. Follow along to understand how different telemetry concepts work.

## Table of Contents

1. [Basic Telemetry](#basic-telemetry)
2. [Baggage Propagation](#baggage-propagation)
3. [Span Links](#span-links)
4. [Advanced Metrics](#advanced-metrics)
5. [Custom Telemetry](#custom-telemetry)
6. [Context Propagation](#context-propagation)
7. [Sampling Strategies](#sampling-strategies)
8. [Error Handling](#error-handling)

## Basic Telemetry

### Getting Started

First, run the application:

```bash
cd OpenTelemetryDemo.AppHost
dotnet run
```

The Aspire Dashboard will open automatically. Note the API URL shown in the dashboard.

### Basic Traces and Metrics

1. **Simple Request Tracing**

   ```bash
   curl http://localhost:5089/WeatherForecast
   ```

   - View in Dashboard: Go to Traces tab
   - Notice: Automatic span creation, duration tracking, and HTTP instrumentation

2. **Custom Metrics**

   ```bash
   curl http://localhost:5089/Metrics/process?complexity=5
   ```

   - View in Dashboard: Go to Metrics tab
   - Look for: `app.requests.total`, `app.processing.time` metrics

## Baggage Propagation

Baggage allows you to propagate context information across service boundaries without modifying every span.

```bash
curl "http://localhost:5089/TelemetryDemo/baggage?userId=john123&tenantId=acme-corp"
```

**What to observe:**

- In the Traces tab, find the "BaggageDemo" trace
- Click through the spans and notice how `user.id` and `tenant.id` are available in child spans
- Baggage is different from span attributes - it propagates automatically

**Key concepts:**

- Baggage is meant for cross-cutting concerns (user ID, tenant ID, request ID)
- It's automatically propagated to all child spans
- Use sparingly as it adds overhead to every request

## Span Links

Span links connect related operations that don't have a parent-child relationship.

```bash
curl "http://localhost:5089/TelemetryDemo/span-links?itemCount=10"
```

**What to observe:**

- Look for the "BatchSummary" span in the traces
- Notice it has links to all individual item processing spans
- This is useful for batch operations, fan-out scenarios, or relating async operations

**Use cases:**

- Batch processing where items are processed independently
- Relating a notification to the original request that triggered it
- Connecting retry attempts to the original operation

## Advanced Metrics

This demo showcases different metric types available in OpenTelemetry.

```bash
curl http://localhost:5089/TelemetryDemo/metrics-demo
```

**Metric Types Demonstrated:**

1. **Counter** - Monotonically increasing value
   - `app.requests.total` - Total requests
   - `app.errors.total` - Total errors

2. **Histogram** - Distribution of values
   - `app.request.duration` - Request durations
   - `app.processing.time` - Processing times

3. **UpDownCounter** - Value that can increase or decrease
   - `app.requests.active` - Currently active requests
   - `app.queue.depth` - Current queue depth

4. **Observable Instruments** - Async observations
   - `app.cpu.usage` - CPU usage percentage
   - `app.threadpool.size` - Thread pool size

**What to observe:**

- In Metrics tab, search for "app." prefix
- Notice how different metric types behave
- UpDownCounters show current state, not cumulative values

## Custom Telemetry

Learn how to add rich telemetry to your operations.

```bash
curl "http://localhost:5089/TelemetryDemo/custom-telemetry?complexity=7"
```

**Features demonstrated:**

- Custom span attributes for business context
- Activity events for marking important moments
- Status codes for indicating success/failure
- Rich error recording

**What to observe:**

- Find the "CustomTelemetryDemo" span
- Expand to see all custom attributes
- Notice the events timeline showing processing stages
- Check how errors are recorded with full context

## Context Propagation

Understanding how trace context flows through async operations.

```bash
curl http://localhost:5089/TelemetryDemo/context-propagation
```

**What to observe:**

- Parent and child spans share the same trace ID
- Context is manually propagated in the async Task.Run
- This is crucial for maintaining trace continuity

**Important for:**

- Background jobs
- Fire-and-forget operations
- Manual thread management

## Sampling Strategies

Configure different sampling strategies to control data volume.

### View Current Configuration

Check `appsettings.json`:

```json
"OpenTelemetry": {
  "Sampling": {
    "Strategy": "default",  // Options: always, never, custom, ratio, default
    "Ratio": 0.1           // For ratio-based sampling
  }
}
```

### Test Different Strategies

1. **Always Sample** (Development)
   - Set strategy to "always"
   - Every request is traced

2. **Ratio-based** (Production)
   - Set strategy to "ratio" with ratio 0.1
   - Only 10% of requests are traced

3. **Custom Sampling**
   - Set strategy to "custom"
   - Uses our custom sampler that:
     - Always samples errors
     - Samples slow operations at 80%
     - Samples health checks at 1%

### Testing Sampling

```bash
# Generate multiple requests
for i in {1..20}; do curl http://localhost:5089/WeatherForecast; done

# Check error sampling (should always be sampled with custom sampler)
curl http://localhost:5089/WeatherForecast/error

# Check slow endpoint sampling (80% with custom sampler)
curl http://localhost:5089/WeatherForecast/slow?delayMs=3000
```

## Error Handling

Proper error telemetry is crucial for debugging.

```bash
# Trigger an error
curl http://localhost:5089/WeatherForecast/error

# Complex error with processing
curl "http://localhost:5089/Metrics/process?complexity=15"
```

**What to observe:**

- Error spans are marked with error status
- Exception details are recorded as events
- Error counters increment
- Stack traces are captured in logs

## Best Practices Demonstrated

1. **Centralized Telemetry Service**
   - Single source of truth for instrumentation
   - Consistent naming across the application
   - Easier testing and maintenance

2. **Semantic Conventions**
   - Using standard attribute names
   - Consistent metric naming
   - Proper activity kinds

3. **Performance Considerations**
   - Appropriate sampling for production
   - Efficient metric recording
   - Avoiding high-cardinality labels

4. **Rich Context**
   - Business-relevant attributes
   - Meaningful span names
   - Proper error recording

## Exploring the Aspire Dashboard

### Traces View

- Use filters to find specific operations
- Click spans to see attributes and events
- Use the timeline to understand flow
- Look for the trace ID in logs

### Metrics View

- Search for specific metrics by name
- Change time ranges to see trends
- Look at metric metadata for descriptions
- Understand different aggregations

### Logs View

- Filter by trace ID to see correlated logs
- Notice structured logging fields
- See how Serilog enrichments appear
- Trace context is automatically included

## Next Steps

1. **Modify the Custom Sampler**
   - Add your own sampling rules
   - Test with different scenarios

2. **Add Your Own Metrics**
   - Create business-specific metrics
   - Add SLI/SLO measurements

3. **Extend Telemetry**
   - Add more span events
   - Create custom resource attributes
   - Implement metric views

4. **Production Considerations**
   - Configure appropriate sampling
   - Set up alerting rules
   - Plan for data retention

