using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Monitoring.Services;

namespace IntegrationPlatform.SFTP.Services;

/// <summary>
/// Decorator that adds telemetry spans to <see cref="ISftpService"/> operations.
/// Useful if underlying implementation has no built-in tracing or when you want to
/// toggle telemetry via DI registration.
/// </summary>
public sealed class SftpServiceTelemetryDecorator : ISftpService
{
    private readonly ISftpService _inner;
    private readonly Tracer _tracer;
    private readonly ILogger<SftpServiceTelemetryDecorator> _logger;

    public SftpServiceTelemetryDecorator(ISftpService inner, ILogger<SftpServiceTelemetryDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
        _tracer = TracerProvider.Default.GetTracer("IntegrationPlatform.SFTP.Decorator");
    }

    public bool IsConnected => _inner.IsConnected;

    public Task<bool> ConnectAsync(string host, int port, string username, string certificatePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "SftpConnect", nameof(ConnectAsync), () => _inner.ConnectAsync(host, port, username, certificatePath, cancellationToken), ("host", host));

    public Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "SftpUpload", nameof(UploadFileAsync), () => _inner.UploadFileAsync(localPath, remotePath, cancellationToken));

    public Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "SftpDownload", nameof(DownloadFileAsync), () => _inner.DownloadFileAsync(remotePath, localPath, cancellationToken));

    public Task<bool> DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "SftpDelete", nameof(DeleteFileAsync), () => _inner.DeleteFileAsync(remotePath, cancellationToken));

    public Task<List<string>> ListFilesAsync(string remotePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "SftpList", nameof(ListFilesAsync), () => _inner.ListFilesAsync(remotePath, cancellationToken));

    public Task<bool> DownloadFilesAsync(IEnumerable<(string remotePath, string localPath)> files, int maxDegreeOfParallelism = 4, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "SftpDownloadMany", nameof(DownloadFilesAsync), () => _inner.DownloadFilesAsync(files, maxDegreeOfParallelism, cancellationToken));

    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "SftpDisconnect", nameof(DisconnectAsync), () => _inner.DisconnectAsync(cancellationToken));
}
