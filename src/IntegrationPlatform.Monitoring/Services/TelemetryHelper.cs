using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Monitoring.Services;

/// <summary>
/// Lightweight helper to wrap common telemetry patterns (trace + log + error handling)
/// so that business services remain terse.
/// </summary>
public static class TelemetryHelper
{
    /// <summary>
    /// Executes an asynchronous action inside an OpenTelemetry <see cref="Activity"/>,
    /// logs start/stop messages, measures duration and surfaces any exceptions in a
    /// consistent way.  Intended to be used with DI-resolved <see cref="ILogger"/>.
    /// </summary>
    /// <typeparam name="TResult">Return type of the action.</typeparam>
    /// <param name="tracer">OpenTelemetry tracer.</param>
    /// <param name="logger">Application logger.</param>
    /// <param name="activityName">Name of the span/activity.</param>
    /// <param name="operation">Semantic operation name (will be added as tag).</param>
    /// <param name="action">Business logic delegate.</param>
    /// <param name="tags">Optional additional span tags.</param>
    /// <returns>Result of the delegate.</returns>
    public static async Task<TResult> TrackAsync<TResult>(this Tracer tracer,
                                                          ILogger logger,
                                                          string activityName,
                                                          string operation,
                                                          Func<Task<TResult>> action,
                                                          params (string Key, object? Value)[] tags)
    {
        using var activity = tracer.StartActivity(activityName, ActivityKind.Internal);
        activity?.SetTag("operation", operation);
        foreach (var (key, val) in tags)
        {
            activity?.SetTag(key, val);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("Started {Activity}", activityName);
            var result = await action();

            activity?.SetStatus(ActivityStatusCode.Ok);
            logger.LogInformation("Completed {Activity} in {ElapsedMs} ms", activityName, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "{Activity} failed", activityName);
            throw;
        }
        finally
        {
            sw.Stop();
            activity?.SetTag("durationMs", sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Overload for actions returning <see cref="Task"/>.
    /// </summary>
    public static async Task TrackAsync(this Tracer tracer,
                                        ILogger logger,
                                        string activityName,
                                        string operation,
                                        Func<Task> action,
                                        params (string Key, object? Value)[] tags)
    {
        await tracer.TrackAsync<object?>(logger, activityName, operation, async () => { await action(); return null; }, tags);
    }
}
