using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using IntegrationPlatform.Monitoring.Services;
using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Infrastructure.FileSystem;

namespace IntegrationPlatform.SFTP.Services
{
    public class SftpService : ISftpService
    {
        private readonly ILogger<SftpService> _logger;
        private readonly IFileService _fileService;
        private readonly Tracer _tracer;
        private SshNet.SftpClient _sftpClient;

        /// <summary>
        /// Indicates whether the client is currently connected to the SFTP server.
        /// </summary>
        public bool IsConnected => _sftpClient?.IsConnected ?? false;

        public SftpService(ILogger<SftpService> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
            _tracer = TracerProvider.Default.GetTracer("IntegrationPlatform.SFTP");
        }

        public async Task<bool> ConnectAsync(string host, int port, string username, string certificatePath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _tracer.TrackAsync(_logger, "SftpConnect", nameof(ConnectAsync), async () =>
                {
                    var certificateBytes = await File.ReadAllBytesAsync(certificatePath, cancellationToken);
                    var keyFile = new SshNet.PrivateKeyFile(new MemoryStream(certificateBytes));

                    _sftpClient = new SshNet.SftpClient(host, port, username, keyFile);
                    _sftpClient.Connect();
                    return true;
                }, ("host", host), ("port", port));
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default)
        {
            try
            {
                await _tracer.TrackAsync(_logger, "SftpUpload", "UploadFile", async () =>
            {
                _logger.LogInformation($"Uploading file from {localPath} to {remotePath}");

                if (!await _fileService.FileExistsAsync(localPath, cancellationToken))
                {
                    throw new FileNotFoundException($"Local file not found: {localPath}");
                }

                using var fileStream = File.OpenRead(localPath);
                _sftpClient.UploadFile(fileStream, remotePath);

                _logger.LogInformation("Successfully uploaded file");
                return true;
            });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return false;
            }
        }

        public async Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _tracer.TrackAsync(_logger, "SftpDownload", nameof(DownloadFileAsync), async () =>
                {
                    using var fileStream = File.Create(localPath);
                    _sftpClient.DownloadFile(remotePath, fileStream);
                    return true;
                }, ("remote", remotePath));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Downloads multiple files from the SFTP server in parallel.
        /// </summary>
        /// <param name="files">Collection of (remotePath, localPath) tuples.</param>
        /// <param name="maxDegreeOfParallelism">Maximum number of concurrent downloads.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if all downloads are successful, otherwise false.</returns>
        public async Task<bool> DownloadFilesAsync(IEnumerable<(string remotePath, string localPath)> files, int maxDegreeOfParallelism = 4, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to SFTP server");
            }

            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var downloadTasks = files.Select(async pair =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await DownloadFileAsync(pair.remotePath, pair.localPath, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            var results = await Task.WhenAll(downloadTasks);
            return results.All(r => r);
        }

        public async Task<bool> DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _tracer.TrackAsync(_logger, "SftpDelete", nameof(DeleteFileAsync), async () =>
                {
                    _sftpClient.DeleteFile(remotePath);
                    return true;
                }, ("remote", remotePath));
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> ListFilesAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _tracer.TrackAsync(_logger, "SftpList", nameof(ListFilesAsync), async () =>
                {
                    var files = _sftpClient.ListDirectory(remotePath)
                        .Where(f => !f.IsDirectory)
                        .Select(f => f.FullName)
                        .ToList();
                    return files;
                }, ("remote", remotePath));
            }
            catch
            {
                return new List<string>();
            }
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Disconnecting from SFTP server");

                _sftpClient?.Disconnect();
                _sftpClient?.Dispose();

                _logger.LogInformation("Successfully disconnected from SFTP server");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from SFTP server");
            }

            return Task.CompletedTask;
        }
    }
} 