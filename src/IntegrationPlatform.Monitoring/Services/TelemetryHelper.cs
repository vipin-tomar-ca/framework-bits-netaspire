using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Monitoring.Services
{
    public static class TelemetryHelper
    {
        public static async Task<T?> TrackAsync<T>(
            this ActivitySource activitySource,
            ILogger logger,
            string activityName,
            string operation,
            Func<Task<T?>> action,
            params (string Key, object? Value)[] tags)
        {
            var stopwatch = Stopwatch.StartNew();

            using var activity = activitySource.StartActivity(activityName, ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("operation.name", operation);
                foreach (var (Key, Value) in tags)
                {
                    if (Value != null)
                        activity.SetTag(Key, Value.ToString());
                }
            }

            logger.LogInformation("Started: {Operation}", operation);

            try
            {
                var result = await action();

                stopwatch.Stop();
                logger.LogInformation("Completed: {Operation} in {Duration}ms", operation, stopwatch.ElapsedMilliseconds);

                if (activity != null)
                {
                    activity.SetTag("execution.duration.ms", stopwatch.ElapsedMilliseconds);
                    activity.SetStatus(ActivityStatusCode.Ok);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "Failed: {Operation} after {Duration}ms", operation, stopwatch.ElapsedMilliseconds);

                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.RecordException(ex);
                }

                throw;
            }
        }

        public static async Task TrackAsync(
            this ActivitySource activitySource,
            ILogger logger,
            string activityName,
            string operation,
            Func<Task> action,
            params (string Key, object? Value)[] tags)
        {
            var stopwatch = Stopwatch.StartNew();

            using var activity = activitySource.StartActivity(activityName, ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("operation.name", operation);
                foreach (var (Key, Value) in tags)
                {
                    if (Value != null)
                        activity.SetTag(Key, Value.ToString());
                }
            }

            logger.LogInformation("Started: {Operation}", operation);

            try
            {
                await action();

                stopwatch.Stop();
                logger.LogInformation("Completed: {Operation} in {Duration}ms", operation, stopwatch.ElapsedMilliseconds);

                if (activity != null)
                {
                    activity.SetTag("execution.duration.ms", stopwatch.ElapsedMilliseconds);
                    activity.SetStatus(ActivityStatusCode.Ok);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogError(ex, "Failed: {Operation} after {Duration}ms", operation, stopwatch.ElapsedMilliseconds);

                if (activity != null)
                {
                    activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                    activity.RecordException(ex);
                }

                throw;
            }
        }
    }
}