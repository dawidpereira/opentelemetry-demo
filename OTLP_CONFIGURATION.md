# OTLP Configuration Guide

This guide explains how to configure OpenTelemetry Protocol (OTLP) export in this demo application.

## Default Configuration with .NET Aspire

When running with .NET Aspire, OTLP endpoints are **automatically configured**. Aspire sets the following environment variables:

- `OTEL_EXPORTER_OTLP_ENDPOINT` - The OTLP endpoint URL (e.g., `http://localhost:4317`)
- `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL` - Dashboard-specific endpoint

No manual configuration is needed when using Aspire!

## Manual OTLP Configuration

To export telemetry to an external OpenTelemetry Collector:

### 1. Environment Variable (Recommended)

Set the OTLP endpoint via environment variable:

```bash
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
dotnet run
```

### 2. Configuration File

Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

### 3. Docker Compose Example

When using external collectors:

```yaml
services:
  api:
    environment:
      - OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
      - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

## OTLP Export Formats

The application supports:

- **gRPC** (default) - Port 4317
- **HTTP** - Port 4318

## Viewing Telemetry Data

### With Aspire Dashboard (Default)
1. Run `dotnet run` in the AppHost project
2. Dashboard opens automatically
3. All telemetry flows to the dashboard

### With External Collectors
1. Configure OTLP endpoint as shown above
2. Ensure your collector is running
3. View data in your backend (Jaeger, Prometheus, etc.)

## Sampling Configuration

Control how much data is exported:

```json
{
  "OpenTelemetry": {
    "Sampling": {
      "Strategy": "ratio",    // Options: always, never, custom, ratio, default
      "Ratio": 0.1           // Sample 10% of traces
    }
  }
}
```

## Troubleshooting OTLP Export

### No Data Exported?

1. Check if OTLP endpoint is set:
   ```bash
   echo $OTEL_EXPORTER_OTLP_ENDPOINT
   ```

2. Verify collector is running:
   ```bash
   curl -v http://localhost:4317
   ```

3. Check application logs for export errors

### Common Issues

- **Connection Refused**: Collector not running or wrong port
- **No Data in Dashboard**: Check sampling configuration
- **High Memory Usage**: Reduce sampling ratio

## Production Recommendations

1. Use environment variables for configuration
2. Set appropriate sampling rates (typically 1-10%)
3. Use secure endpoints (HTTPS/TLS)
4. Monitor collector health
5. Configure batch export settings for performance

## Example Collector Configuration

Basic OpenTelemetry Collector config (`otel-collector.yaml`):

```yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

exporters:
  logging:
    loglevel: debug
  
  prometheus:
    endpoint: "0.0.0.0:8889"
  
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true

processors:
  batch:

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging, jaeger]
    
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging, prometheus]
    
    logs:
      receivers: [otlp]
      processors: [batch]
      exporters: [logging]
```

## Key Concepts

1. **OTLP** is the standard protocol for sending telemetry data
2. **Aspire** automatically configures OTLP for local development
3. **Environment variables** override configuration files
4. **Sampling** controls data volume and costs
5. **Collectors** can transform and route data to multiple backends