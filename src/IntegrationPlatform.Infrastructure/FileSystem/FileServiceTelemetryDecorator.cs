using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using IntegrationPlatform.Monitoring.Services;

namespace IntegrationPlatform.Infrastructure.FileSystem;

/// <summary>
/// Decorator that adds OpenTelemetry + logging instrumentation to any underlying
/// <see cref="IFileService"/> implementation without changing its code.
/// Register via DI:
///
/// services.AddSingleton<IFileService, FileService>();
/// services.Decorate<IFileService, FileServiceTelemetryDecorator>();
/// </summary>
public sealed class FileServiceTelemetryDecorator : IFileService
{
    private readonly IFileService _inner;
    private readonly Tracer _tracer;
    private readonly ILogger<FileServiceTelemetryDecorator> _logger;

    public FileServiceTelemetryDecorator(IFileService inner,
                                         ILogger<FileServiceTelemetryDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
        _tracer = TracerProvider.Default.GetTracer("IntegrationPlatform.FileSystem");

        // propagate events
        _inner.FileChanged += (s, e) => FileChanged?.Invoke(s, e);
        _inner.FileChangesBuffered += (s, e) => FileChangesBuffered?.Invoke(s, e);
    }

    #region IFileService pass-through with telemetry
    public Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "ReadFile", nameof(ReadFileAsync), () => _inner.ReadFileAsync(filePath, cancellationToken), ("filePath", filePath));

    public Task WriteFileAsync(string filePath, string content, bool append = false, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "WriteFile", nameof(WriteFileAsync), () => _inner.WriteFileAsync(filePath, content, append, cancellationToken), ("filePath", filePath));

    public Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "FileExists", nameof(FileExistsAsync), () => _inner.FileExistsAsync(filePath, cancellationToken), ("filePath", filePath));

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "DeleteFile", nameof(DeleteFileAsync), () => _inner.DeleteFileAsync(filePath, cancellationToken), ("filePath", filePath));

    public Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "CopyFile", nameof(CopyFileAsync), () => _inner.CopyFileAsync(sourcePath, destinationPath, overwrite, cancellationToken),
            ("source", sourcePath), ("dest", destinationPath));

    public Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "MoveFile", nameof(MoveFileAsync), () => _inner.MoveFileAsync(sourcePath, destinationPath, overwrite, cancellationToken),
            ("source", sourcePath), ("dest", destinationPath));

    public Task<byte[]> CompressFileAsync(string filePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "CompressFile", nameof(CompressFileAsync), () => _inner.CompressFileAsync(filePath, cancellationToken), ("filePath", filePath));

    public Task DecompressFileAsync(byte[] compressedData, string destinationPath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "DecompressFile", nameof(DecompressFileAsync), () => _inner.DecompressFileAsync(compressedData, destinationPath, cancellationToken), ("dest", destinationPath));

    public Task<IDisposable> LockFileAsync(string filePath, TimeSpan timeout, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "LockFile", nameof(LockFileAsync), () => _inner.LockFileAsync(filePath, timeout, cancellationToken), ("filePath", filePath));

    public Task<byte[]> EncryptFileAsync(string filePath, string key, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "EncryptFile", nameof(EncryptFileAsync), () => _inner.EncryptFileAsync(filePath, key, cancellationToken), ("filePath", filePath));

    public Task DecryptFileAsync(byte[] encryptedData, string destinationPath, string key, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "DecryptFile", nameof(DecryptFileAsync), () => _inner.DecryptFileAsync(encryptedData, destinationPath, key, cancellationToken), ("dest", destinationPath));

    public Task<string> CalculateChecksumAsync(string filePath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "Checksum", nameof(CalculateChecksumAsync), () => _inner.CalculateChecksumAsync(filePath, cancellationToken), ("filePath", filePath));

    public Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "VerifyChecksum", nameof(VerifyChecksumAsync), () => _inner.VerifyChecksumAsync(filePath, expectedChecksum, cancellationToken), ("filePath", filePath));

    public Task StartWatchingAsync(string directoryPath, FileWatchOptions? options = null, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "StartWatching", nameof(StartWatchingAsync), () => _inner.StartWatchingAsync(directoryPath, options, cancellationToken), ("dir", directoryPath));

    public Task StopWatchingAsync(string directoryPath, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "StopWatching", nameof(StopWatchingAsync), () => _inner.StopWatchingAsync(directoryPath, cancellationToken), ("dir", directoryPath));
    #endregion

    #region events passthrough
    public event EventHandler<FileSystemEventArgs>? FileChanged;
    public event EventHandler<IEnumerable<FileSystemEventArgs>>? FileChangesBuffered;
    #endregion
}
