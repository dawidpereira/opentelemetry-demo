using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OpenTelemetryDemo.Api.Services;

public class TelemetryService
{
    public const string ServiceName = "OpenTelemetryDemo.Api";
    public const string ServiceVersion = "1.0.0";

    public ActivitySource ActivitySource { get; }
    public Meter Meter { get; }

    public Counter<long> RequestCounter { get; }
    public Counter<long> ErrorCounter { get; }

    public Histogram<double> RequestDuration { get; }
    public Histogram<long> ProcessingTime { get; }

    public UpDownCounter<int> ActiveRequests { get; }
    public UpDownCounter<int> QueueDepth { get; }

    public ObservableGauge<double> CpuUsage { get; }
    public ObservableUpDownCounter<int> ThreadPoolSize { get; }

    private int _activeRequests = 0;
    private int _queueDepth = 0;

    public TelemetryService()
    {
        ActivitySource = new ActivitySource(ServiceName, ServiceVersion);
        Meter = new Meter(ServiceName, ServiceVersion);

        RequestCounter = Meter.CreateCounter<long>(
            "app.requests.total",
            unit: "requests",
            description: "Total number of requests received");

        ErrorCounter = Meter.CreateCounter<long>(
            "app.errors.total",
            unit: "errors",
            description: "Total number of errors");

        RequestDuration = Meter.CreateHistogram<double>(
            "app.request.duration",
            unit: "ms",
            description: "Request duration in milliseconds");

        ProcessingTime = Meter.CreateHistogram<long>(
            "app.processing.time",
            unit: "ms",
            description: "Processing time for operations");

        ActiveRequests = Meter.CreateUpDownCounter<int>(
            "app.requests.active",
            unit: "requests",
            description: "Number of requests currently being processed");

        QueueDepth = Meter.CreateUpDownCounter<int>(
            "app.queue.depth",
            unit: "items",
            description: "Current depth of the processing queue");

        CpuUsage = Meter.CreateObservableGauge(
            "app.cpu.usage",
            observeValue: () => GetCpuUsage(),
            unit: "percent",
            description: "Current CPU usage percentage");

        ThreadPoolSize = Meter.CreateObservableUpDownCounter(
            "app.threadpool.size",
            observeValue: () => GetThreadPoolSize(),
            unit: "threads",
            description: "Current thread pool size");
    }

    public void IncrementActiveRequests()
    {
        Interlocked.Increment(ref _activeRequests);
        ActiveRequests.Add(1);
    }

    public void DecrementActiveRequests()
    {
        Interlocked.Decrement(ref _activeRequests);
        ActiveRequests.Add(-1);
    }

    public void UpdateQueueDepth(int delta)
    {
        Interlocked.Add(ref _queueDepth, delta);
        QueueDepth.Add(delta);
    }

    public int GetActiveRequestCount() => _activeRequests;
    public int GetQueueDepth() => _queueDepth;

    private double GetCpuUsage()
    {
        // Demo: Returns random CPU usage for demonstration purposes
        return Random.Shared.Next(10, 90);
    }

    private int GetThreadPoolSize()
    {
        ThreadPool.GetAvailableThreads(out _, out _);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out _);
        ThreadPool.GetMinThreads(out int minWorkerThreads, out _);
        return maxWorkerThreads - minWorkerThreads;
    }
}

