using IntegrationPlatform.Contracts.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Contracts.Interfaces;

public interface IMonitoringService
{
    /// <summary>
    /// Gets the status of an integration job
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>The job status</returns>
    Task<IntegrationJob> GetJobStatusAsync(int jobId);

    /// <summary>
    /// Gets the status of a file transfer
    /// </summary>
    /// <param name="transferId">The ID of the file transfer</param>
    /// <returns>The file transfer status</returns>
    Task<FileTransfer> GetFileTransferStatusAsync(int transferId);

    /// <summary>
    /// Gets the logs for an integration job
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>List of job logs</returns>
    Task<List<IntegrationLog>> GetJobLogsAsync(int jobId);

    /// <summary>
    /// Gets the file transfers for an integration job
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>List of file transfers</returns>
    Task<List<FileTransfer>> GetJobFileTransfersAsync(int jobId);

    /// <summary>
    /// Gets the audit trail entries for an integration job
    /// </summary>
    /// <param name="jobId">The ID of the job</param>
    /// <returns>List of audit trail entries</returns>
    Task<List<AuditTrailEntry>> GetJobAuditTrailAsync(int jobId);

    /// <summary>
    /// Starts a new activity for monitoring
    /// </summary>
    /// <param name="name">The name of the activity</param>
    /// <param name="operation">The operation being performed</param>
    /// <returns>The activity</returns>
    Task<Activity> StartActivityAsync(string name, string operation);

    /// <summary>
    /// Ends an activity and records its status
    /// </summary>
    /// <param name="activity">The activity to end</param>
    /// <param name="status">The status of the activity</param>
    Task EndActivityAsync(Activity activity, string status);

    /// <summary>
    /// Logs a metric
    /// </summary>
    /// <param name="name">The name of the metric</param>
    /// <param name="value">The value of the metric</param>
    /// <param name="tags">Optional tags for the metric</param>
    Task LogMetricAsync(string name, double value, Dictionary<string, string>? tags = null);

    /// <summary>
    /// Logs an event
    /// </summary>
    /// <param name="name">The name of the event</param>
    /// <param name="message">The event message</param>
    /// <param name="level">The log level</param>
    Task LogEventAsync(string name, string message, LogLevel level = LogLevel.Information);
} 