using OpenTelemetry.Trace;

namespace OpenTelemetryDemo.ServiceDefaults;

/// <summary>
/// Custom sampler that demonstrates different sampling strategies
/// </summary>
public class CustomSampler : Sampler
{
    private readonly Random _random = new();

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        // Always sample if there's a parent span that was sampled
        if (samplingParameters.ParentContext != default && samplingParameters.ParentContext.IsRemote)
        {
            return new SamplingResult(samplingParameters.ParentContext.TraceFlags.HasFlag(System.Diagnostics.ActivityTraceFlags.Recorded));
        }

        // Extract operation name from tags
        var operationName = samplingParameters.Name;

        // Always sample error operations
        if (operationName.Contains("error", StringComparison.OrdinalIgnoreCase))
        {
            return new SamplingResult(SamplingDecision.RecordAndSample);
        }

        // Sample slow operations at higher rate
        if (operationName.Contains("slow", StringComparison.OrdinalIgnoreCase))
        {
            return new SamplingResult(_random.NextDouble() < 0.8 ? SamplingDecision.RecordAndSample : SamplingDecision.Drop);
        }

        // Sample health checks at very low rate
        if (operationName.Contains("health", StringComparison.OrdinalIgnoreCase))
        {
            return new SamplingResult(_random.NextDouble() < 0.01 ? SamplingDecision.RecordAndSample : SamplingDecision.Drop);
        }

        // Check for custom sampling hint in tags
        if (samplingParameters.Tags != null)
        {
            foreach (var tag in samplingParameters.Tags)
            {
                if (tag.Key == "sampling.priority" && tag.Value is int priority)
                {
                    // Priority 0 = never sample, 1 = normal rate, 2 = always sample
                    return priority switch
                    {
                        0 => new SamplingResult(SamplingDecision.Drop),
                        2 => new SamplingResult(SamplingDecision.RecordAndSample),
                        _ => new SamplingResult(_random.NextDouble() < 0.1 ? SamplingDecision.RecordAndSample : SamplingDecision.Drop)
                    };
                }
            }
        }

        // Default sampling rate of 10%
        return new SamplingResult(_random.NextDouble() < 0.1 ? SamplingDecision.RecordAndSample : SamplingDecision.Drop);
    }
}

