using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Infrastructure.FileSystem;

namespace IntegrationPlatform.SFTP.Services
{
    public class SftpService : ISftpService
    {
        private readonly ILogger<SftpService> _logger;
        private readonly IFileService _fileService;
        private SshNet.SftpClient _sftpClient;

        public SftpService(ILogger<SftpService> logger, IFileService fileService)
        {
            _logger = logger;
            _fileService = fileService;
        }

        public async Task<bool> ConnectAsync(string host, int port, string username, string certificatePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Connecting to SFTP server: {host}:{port}");

                var certificateBytes = await File.ReadAllBytesAsync(certificatePath, cancellationToken);
                var keyFile = new SshNet.PrivateKeyFile(new MemoryStream(certificateBytes));

                _sftpClient = new SshNet.SftpClient(host, port, username, keyFile);
                _sftpClient.Connect();

                _logger.LogInformation("Successfully connected to SFTP server");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to SFTP server");
                return false;
            }
        }

        public async Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default)
        {
            try
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
                _logger.LogInformation($"Downloading file from {remotePath} to {localPath}");

                using var fileStream = File.Create(localPath);
                _sftpClient.DownloadFile(remotePath, fileStream);

                _logger.LogInformation("Successfully downloaded file");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                return false;
            }
        }

        public async Task<bool> DeleteFileAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Deleting remote file: {remotePath}");

                _sftpClient.DeleteFile(remotePath);

                _logger.LogInformation("Successfully deleted remote file");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting remote file");
                return false;
            }
        }

        public async Task<string[]> ListFilesAsync(string remotePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Listing files in remote directory: {remotePath}");

                var files = _sftpClient.ListDirectory(remotePath)
                    .Where(f => !f.IsDirectory)
                    .Select(f => f.FullName)
                    .ToArray();

                _logger.LogInformation($"Found {files.Length} files");
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing remote files");
                return Array.Empty<string>();
            }
        }

        public void Disconnect()
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
        }
    }
} 