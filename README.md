# OpenTelemetry Demo with .NET 9, Serilog, and .NET Aspire

This project demonstrates a comprehensive OpenTelemetry setup using .NET 9, Serilog, and .NET Aspire with its built-in observability dashboard.

## Features

- **.NET 9** ASP.NET Core Web API
- **OpenTelemetry** integration for traces, metrics, and logs
- **Serilog** with OpenTelemetry sink for structured logging
- **.NET Aspire** for local development orchestration with built-in observability dashboard
- **Scalar** API documentation UI for easy API testing and exploration
- All telemetry (logs, traces, metrics) automatically routed to Aspire Dashboard

## Comprehensive Feature Demonstrations

This project demonstrates many OpenTelemetry concepts:

- **Baggage Propagation** - Cross-cutting context propagation
- **Span Links** - Relating non-parent-child operations
- **Advanced Metrics** - Counters, Histograms, UpDownCounters, Observable instruments
- **Custom Sampling** - Intelligent trace sampling strategies
- **Context Propagation** - Manual trace context handling
- **Rich Telemetry** - Custom attributes, events, and status codes

📚 **[See the Complete OpenTelemetry Feature Guide](TELEMETRY_GUIDE.md)** for a hands-on tour of all features.

📡 **[OTLP Configuration Guide](OTLP_CONFIGURATION.md)** - Learn how to configure OTLP export for logs, traces, and metrics.

## Prerequisites

- .NET 9 SDK
- Docker and Docker Compose
- Visual Studio 2022 or VS Code (optional)

## Project Structure

```
OpenTelemetryDemo/
├── OpenTelemetryDemo.Api/               # ASP.NET Core API project
├── OpenTelemetryDemo.ServiceDefaults/   # Shared OpenTelemetry configuration
├── OpenTelemetryDemo.AppHost/           # .NET Aspire orchestrator
└── requests.http                       # Sample HTTP requests
```

## Quick Start

### 1. Run the Application with .NET Aspire

```bash
cd OpenTelemetryDemo.AppHost
dotnet run
```

This will:

- Start the Aspire Dashboard (automatically opens in browser)
- Launch the API project
- Configure all telemetry to flow to the Aspire Dashboard
- The API will be available at the port shown in the Aspire Dashboard

**Note**: When you see the error about missing environment variables, the launch settings file will configure them automatically on the next run.

### 2. Generate Telemetry Data

Use the provided `requests.http` file with VS Code REST Client extension or any HTTP client:

```bash
# Get weather forecast
curl http://localhost:5000/WeatherForecast

# Simulate error
curl http://localhost:5000/WeatherForecast/error

# Test slow endpoint
curl http://localhost:5000/WeatherForecast/slow?delayMs=3000

# Process with metrics
curl http://localhost:5000/Metrics/process?complexity=2
```

### 3. Access the API Documentation

Navigate to `/scalar/v1` on your API endpoint (check the Aspire Dashboard for the port) to access the Scalar API documentation UI. For example:

- `http://localhost:5089/scalar/v1`

Scalar provides:

- Interactive API documentation
- Built-in API testing capabilities
- Code generation for multiple languages
- Beautiful, modern UI

### 4. View Telemetry in Aspire Dashboard

The Aspire Dashboard automatically opens when you run the application. You can view:

- **Structured Logs**: All Serilog logs with trace correlation
- **Distributed Traces**: Full request traces with spans
- **Metrics**: Real-time metrics for your application
- **Resources**: Status of your running services

## Key Components

### OpenTelemetry Configuration

The `ServiceDefaults` project contains centralized OpenTelemetry configuration:

- **Traces**: ASP.NET Core and HTTP client instrumentation
- **Metrics**: Runtime, process, and custom metrics
- **Logs**: Serilog integration with OpenTelemetry sink

### Custom Instrumentation

The API includes examples of:

- Custom ActivitySource for distributed tracing
- Custom Meters for business metrics
- Structured logging with trace correlation
- Error handling with proper telemetry

### Endpoints

#### Weather Forecast

- `GET /WeatherForecast` - Get weather forecast
- `GET /WeatherForecast/{date}` - Get forecast for specific date
- `GET /WeatherForecast/error` - Simulate error
- `GET /WeatherForecast/slow` - Simulate slow response

#### Metrics Demo

- `GET /Metrics/process` - Process with custom metrics
- `GET /Metrics/batch` - Batch processing example
- `GET /Metrics/status` - Application status

#### Advanced Telemetry Demos

- `GET /TelemetryDemo/baggage` - Demonstrates baggage propagation
- `GET /TelemetryDemo/span-links` - Shows span linking for batch operations
- `GET /TelemetryDemo/metrics-demo` - Showcases all metric types
- `GET /TelemetryDemo/custom-telemetry` - Rich telemetry with events and attributes
- `GET /TelemetryDemo/context-propagation` - Manual context propagation

#### System

- `GET /health` - Health check
- `GET /scalar/v1` - Interactive API documentation

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment name (Development/Production)
- Aspire automatically configures `OTEL_EXPORTER_OTLP_ENDPOINT` when running

### Serilog Configuration

Serilog is configured to:

- Write to console with structured output
- Send logs to OpenTelemetry collector via OTLP
- Include trace and span IDs for correlation
- Enrich logs with environment and thread information

## Troubleshooting

### No data in Aspire Dashboard?

1. Ensure the application is running through the AppHost project
2. Check the console output for any errors
3. Verify you're making requests to generate telemetry
4. The dashboard should automatically receive all telemetry data

### Build errors?

Ensure you have .NET 9 SDK installed: `dotnet --version`

## Advanced Usage

### Adding Custom Metrics

```csharp
var meter = new Meter("YourApp", "1.0.0");
var counter = meter.CreateCounter<long>("custom_metric");
counter.Add(1, new("tag", "value"));
```

### Adding Custom Traces

```csharp
using var activity = ActivitySource.StartActivity("CustomOperation");
activity?.SetTag("custom.tag", "value");
```

### Benefits of Using Aspire Dashboard

- **Zero Configuration**: Aspire automatically configures OTLP endpoints
- **Integrated Experience**: See logs, traces, and metrics in one place
- **Development Focused**: Optimized for local development scenarios
- **Real-time Updates**: Live streaming of telemetry data
- **Correlation**: Easy navigation between related logs and traces

## License

This is a demo project for educational purposes.

