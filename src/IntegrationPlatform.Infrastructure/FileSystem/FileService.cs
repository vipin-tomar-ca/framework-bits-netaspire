using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using IntegrationPlatform.Infrastructure.ErrorHandling;

namespace IntegrationPlatform.Infrastructure.FileSystem
{
    public class FileSystemEventArgs : EventArgs
    {
        public string FilePath { get; }
        public FileSystemChangeType ChangeType { get; }
        public DateTime Timestamp { get; }
        public long FileSize { get; }
        public string FileExtension { get; }

        public FileSystemEventArgs(string filePath, FileSystemChangeType changeType, long fileSize = 0)
        {
            FilePath = filePath;
            ChangeType = changeType;
            Timestamp = DateTime.UtcNow;
            FileSize = fileSize;
            FileExtension = Path.GetExtension(filePath).ToLowerInvariant();
        }
    }

    public class FileWatchOptions
    {
        public string[]? FileExtensions { get; set; }
        public long? MinFileSize { get; set; }
        public long? MaxFileSize { get; set; }
        public TimeSpan? DebounceInterval { get; set; }
        public int? BufferSize { get; set; }
        public TimeSpan? BufferInterval { get; set; }
        public int? MaxChangesPerMinute { get; set; }
        public bool IncludeSubdirectories { get; set; } = true;
    }

    public enum FileSystemChangeType
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }

    public interface IFileService
    {
        Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task WriteFileAsync(string filePath, string content, bool append = false, CancellationToken cancellationToken = default);
        Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default);
        Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);
        Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default);
        Task<byte[]> CompressFileAsync(string filePath, CancellationToken cancellationToken = default);
        Task DecompressFileAsync(byte[] compressedData, string destinationPath, CancellationToken cancellationToken = default);
        Task<IDisposable> LockFileAsync(string filePath, TimeSpan timeout, CancellationToken cancellationToken = default);
        Task<byte[]> EncryptFileAsync(string filePath, string key, CancellationToken cancellationToken = default);
        Task DecryptFileAsync(byte[] encryptedData, string destinationPath, string key, CancellationToken cancellationToken = default);
        Task<string> CalculateChecksumAsync(string filePath, CancellationToken cancellationToken = default);
        Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum, CancellationToken cancellationToken = default);
        Task StartWatchingAsync(string directoryPath, FileWatchOptions? options = null, CancellationToken cancellationToken = default);
        Task StopWatchingAsync(string directoryPath, CancellationToken cancellationToken = default);
        event EventHandler<FileSystemEventArgs> FileChanged;
        event EventHandler<IEnumerable<FileSystemEventArgs>> FileChangesBuffered;
    }

    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly ErrorHandler _errorHandler;
        private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);
        private const int KeySize = 256;
        private const int BlockSize = 128;
        private const int Iterations = 10000;
        private readonly Dictionary<string, (FileSystemWatcher Watcher, FileWatchOptions Options, Timer? DebounceTimer, Timer? BufferTimer, ConcurrentQueue<FileSystemEventArgs> ChangeBuffer)> _watchers = new();
        private readonly object _watchersLock = new();
        private readonly Dictionary<string, int> _changeCounters = new();
        private readonly Timer _changeCounterResetTimer;

        public event EventHandler<FileSystemEventArgs>? FileChanged;
        public event EventHandler<IEnumerable<FileSystemEventArgs>>? FileChangesBuffered;

        public FileService(ILogger<FileService> logger, ErrorHandler errorHandler)
        {
            _logger = logger;
            _errorHandler = errorHandler;
            _changeCounterResetTimer = new Timer(ResetChangeCounters, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public async Task<string> ReadFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Reading file: {filePath}");
                
                if (!await FileExistsAsync(filePath, cancellationToken))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                
                _logger.LogInformation($"Successfully read file: {filePath}");
                return content;
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.ReadFileAsync: {filePath}");
                _logger.LogError($"Error reading file {filePath}. Error ID: {errorId}");
                throw;
            }
        }

        public async Task WriteFileAsync(string filePath, string content, bool append = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Writing to file: {filePath} (Append: {append})");

                // Ensure directory exists
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var writer = new StreamWriter(filePath, append, Encoding.UTF8);
                await writer.WriteAsync(content);
                await writer.FlushAsync();
                
                _logger.LogInformation($"Successfully wrote to file: {filePath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.WriteFileAsync: {filePath}");
                _logger.LogError($"Error writing to file {filePath}. Error ID: {errorId}");
                throw;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                return await Task.Run(() => File.Exists(filePath), cancellationToken);
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.FileExistsAsync: {filePath}");
                _logger.LogError($"Error checking file existence {filePath}. Error ID: {errorId}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Deleting file: {filePath}");

                if (!await FileExistsAsync(filePath, cancellationToken))
                {
                    _logger.LogWarning($"File not found for deletion: {filePath}");
                    return;
                }

                await Task.Run(() => File.Delete(filePath), cancellationToken);
                _logger.LogInformation($"Successfully deleted file: {filePath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.DeleteFileAsync: {filePath}");
                _logger.LogError($"Error deleting file {filePath}. Error ID: {errorId}");
                throw;
            }
        }

        public async Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Copying file from {sourcePath} to {destinationPath}");

                if (!await FileExistsAsync(sourcePath, cancellationToken))
                {
                    throw new FileNotFoundException($"Source file not found: {sourcePath}");
                }

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite), cancellationToken);
                _logger.LogInformation($"Successfully copied file to {destinationPath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.CopyFileAsync: {sourcePath} -> {destinationPath}");
                _logger.LogError($"Error copying file. Error ID: {errorId}");
                throw;
            }
        }

        public async Task MoveFileAsync(string sourcePath, string destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Moving file from {sourcePath} to {destinationPath}");

                if (!await FileExistsAsync(sourcePath, cancellationToken))
                {
                    throw new FileNotFoundException($"Source file not found: {sourcePath}");
                }

                if (await FileExistsAsync(destinationPath, cancellationToken) && !overwrite)
                {
                    throw new IOException($"Destination file already exists: {destinationPath}");
                }

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await Task.Run(() => File.Move(sourcePath, destinationPath, overwrite), cancellationToken);
                _logger.LogInformation($"Successfully moved file to {destinationPath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.MoveFileAsync: {sourcePath} -> {destinationPath}");
                _logger.LogError($"Error moving file. Error ID: {errorId}");
                throw;
            }
        }

        public async Task<byte[]> CompressFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Compressing file: {filePath}");

                if (!await FileExistsAsync(filePath, cancellationToken))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                    var entry = archive.CreateEntry(Path.GetFileName(filePath));
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(fileBytes, cancellationToken);
                }

                _logger.LogInformation($"Successfully compressed file: {filePath}");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.CompressFileAsync: {filePath}");
                _logger.LogError($"Error compressing file. Error ID: {errorId}");
                throw;
            }
        }

        public async Task DecompressFileAsync(byte[] compressedData, string destinationPath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Decompressing file to: {destinationPath}");

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var memoryStream = new MemoryStream(compressedData);
                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
                var entry = archive.Entries.First();
                using var entryStream = entry.Open();
                using var fileStream = new FileStream(destinationPath, FileMode.Create);
                await entryStream.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation($"Successfully decompressed file to: {destinationPath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.DecompressFileAsync: {destinationPath}");
                _logger.LogError($"Error decompressing file. Error ID: {errorId}");
                throw;
            }
        }

        public async Task<IDisposable> LockFileAsync(string filePath, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Attempting to lock file: {filePath}");

                var lockAcquired = await _fileLock.WaitAsync(timeout, cancellationToken);
                if (!lockAcquired)
                {
                    throw new TimeoutException($"Failed to acquire lock for file: {filePath}");
                }

                _logger.LogInformation($"Successfully locked file: {filePath}");
                return new FileLock(_fileLock);
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.LockFileAsync: {filePath}");
                _logger.LogError($"Error locking file. Error ID: {errorId}");
                throw;
            }
        }

        public async Task<byte[]> EncryptFileAsync(string filePath, string key, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Encrypting file: {filePath}");

                if (!await FileExistsAsync(filePath, cancellationToken))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;

                // Generate salt and IV
                var salt = new byte[16];
                using (var rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }
                aes.GenerateIV();

                // Derive key
                using var deriveBytes = new Rfc2898DeriveBytes(key, salt, Iterations);
                aes.Key = deriveBytes.GetBytes(32);

                using var memoryStream = new MemoryStream();
                await memoryStream.WriteAsync(salt, 0, salt.Length, cancellationToken);
                await memoryStream.WriteAsync(aes.IV, 0, aes.IV.Length, cancellationToken);

                using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    await cryptoStream.WriteAsync(fileBytes, 0, fileBytes.Length, cancellationToken);
                }

                _logger.LogInformation($"Successfully encrypted file: {filePath}");
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.EncryptFileAsync: {filePath}");
                _logger.LogError($"Error encrypting file. Error ID: {errorId}");
                throw;
            }
        }

        public async Task DecryptFileAsync(byte[] encryptedData, string destinationPath, string key, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Decrypting file to: {destinationPath}");

                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var memoryStream = new MemoryStream(encryptedData);
                using var aes = Aes.Create();
                aes.KeySize = KeySize;
                aes.BlockSize = BlockSize;

                // Read salt and IV
                var salt = new byte[16];
                await memoryStream.ReadAsync(salt, 0, salt.Length, cancellationToken);
                var iv = new byte[16];
                await memoryStream.ReadAsync(iv, 0, iv.Length, cancellationToken);
                aes.IV = iv;

                // Derive key
                using var deriveBytes = new Rfc2898DeriveBytes(key, salt, Iterations);
                aes.Key = deriveBytes.GetBytes(32);

                using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using var fileStream = new FileStream(destinationPath, FileMode.Create);
                await cryptoStream.CopyToAsync(fileStream, cancellationToken);

                _logger.LogInformation($"Successfully decrypted file to: {destinationPath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.DecryptFileAsync: {destinationPath}");
                _logger.LogError($"Error decrypting file. Error ID: {errorId}");
                throw;
            }
        }

        public async Task<string> CalculateChecksumAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Calculating checksum for file: {filePath}");

                if (!await FileExistsAsync(filePath, cancellationToken))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
                var checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                _logger.LogInformation($"Successfully calculated checksum for file: {filePath}");
                return checksum;
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.CalculateChecksumAsync: {filePath}");
                _logger.LogError($"Error calculating checksum. Error ID: {errorId}");
                throw;
            }
        }

        public async Task<bool> VerifyChecksumAsync(string filePath, string expectedChecksum, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Verifying checksum for file: {filePath}");

                var actualChecksum = await CalculateChecksumAsync(filePath, cancellationToken);
                var isValid = string.Equals(actualChecksum, expectedChecksum, StringComparison.OrdinalIgnoreCase);

                _logger.LogInformation($"Checksum verification {(isValid ? "passed" : "failed")} for file: {filePath}");
                return isValid;
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.VerifyChecksumAsync: {filePath}");
                _logger.LogError($"Error verifying checksum. Error ID: {errorId}");
                throw;
            }
        }

        public async Task StartWatchingAsync(string directoryPath, FileWatchOptions? options = null, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Starting to watch directory: {directoryPath}");

                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
                }

                options ??= new FileWatchOptions();

                var watcher = new FileSystemWatcher(directoryPath)
                {
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = options.IncludeSubdirectories,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName |
                                 NotifyFilters.LastWrite | NotifyFilters.Size
                };

                var changeBuffer = new ConcurrentQueue<FileSystemEventArgs>();
                Timer? debounceTimer = null;
                Timer? bufferTimer = null;

                if (options.DebounceInterval.HasValue)
                {
                    debounceTimer = new Timer(_ => ProcessDebouncedChanges(directoryPath), null, Timeout.Infinite, Timeout.Infinite);
                }

                if (options.BufferInterval.HasValue)
                {
                    bufferTimer = new Timer(_ => ProcessBufferedChanges(directoryPath), null, options.BufferInterval.Value, options.BufferInterval.Value);
                }

                watcher.Created += (s, e) => OnFileChanged(e.FullPath, FileSystemChangeType.Created, options, directoryPath);
                watcher.Changed += (s, e) => OnFileChanged(e.FullPath, FileSystemChangeType.Modified, options, directoryPath);
                watcher.Deleted += (s, e) => OnFileChanged(e.FullPath, FileSystemChangeType.Deleted, options, directoryPath);
                watcher.Renamed += (s, e) => OnFileChanged(e.FullPath, FileSystemChangeType.Renamed, options, directoryPath);

                lock (_watchersLock)
                {
                    if (_watchers.ContainsKey(directoryPath))
                    {
                        var (oldWatcher, _, oldDebounceTimer, oldBufferTimer, _) = _watchers[directoryPath];
                        oldWatcher.Dispose();
                        oldDebounceTimer?.Dispose();
                        oldBufferTimer?.Dispose();
                    }
                    _watchers[directoryPath] = (watcher, options, debounceTimer, bufferTimer, changeBuffer);
                }

                _logger.LogInformation($"Successfully started watching directory: {directoryPath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.StartWatchingAsync: {directoryPath}");
                _logger.LogError($"Error starting directory watch. Error ID: {errorId}");
                throw;
            }
        }

        private void OnFileChanged(string filePath, FileSystemChangeType changeType, FileWatchOptions options, string directoryPath)
        {
            try
            {
                // Check file extension filter
                if (options.FileExtensions?.Length > 0)
                {
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    if (!options.FileExtensions.Contains(extension))
                    {
                        return;
                    }
                }

                // Check file size limits
                if (options.MinFileSize.HasValue || options.MaxFileSize.HasValue)
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Exists)
                    {
                        if (options.MinFileSize.HasValue && fileInfo.Length < options.MinFileSize.Value)
                        {
                            return;
                        }
                        if (options.MaxFileSize.HasValue && fileInfo.Length > options.MaxFileSize.Value)
                        {
                            return;
                        }
                    }
                }

                // Check change rate limit
                if (options.MaxChangesPerMinute.HasValue)
                {
                    lock (_changeCounters)
                    {
                        if (!_changeCounters.ContainsKey(directoryPath))
                        {
                            _changeCounters[directoryPath] = 0;
                        }
                        if (_changeCounters[directoryPath] >= options.MaxChangesPerMinute.Value)
                        {
                            _logger.LogWarning($"Change rate limit exceeded for directory: {directoryPath}");
                            return;
                        }
                        _changeCounters[directoryPath]++;
                    }
                }

                var fileSize = 0L;
                if (File.Exists(filePath))
                {
                    fileSize = new FileInfo(filePath).Length;
                }

                var args = new FileSystemEventArgs(filePath, changeType, fileSize);

                if (options.DebounceInterval.HasValue)
                {
                    lock (_watchersLock)
                    {
                        if (_watchers.TryGetValue(directoryPath, out var watcherInfo))
                        {
                            watcherInfo.ChangeBuffer.Enqueue(args);
                            watcherInfo.DebounceTimer?.Change(options.DebounceInterval.Value, Timeout.InfiniteTimeSpan);
                        }
                    }
                }
                else if (options.BufferInterval.HasValue)
                {
                    lock (_watchersLock)
                    {
                        if (_watchers.TryGetValue(directoryPath, out var watcherInfo))
                        {
                            watcherInfo.ChangeBuffer.Enqueue(args);
                        }
                    }
                }
                else
                {
                    FileChanged?.Invoke(this, args);
                }

                _logger.LogInformation($"File change detected: {changeType} - {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling file change event: {filePath}");
            }
        }

        private void ProcessDebouncedChanges(string directoryPath)
        {
            try
            {
                lock (_watchersLock)
                {
                    if (_watchers.TryGetValue(directoryPath, out var watcherInfo))
                    {
                        while (watcherInfo.ChangeBuffer.TryDequeue(out var args))
                        {
                            FileChanged?.Invoke(this, args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing debounced changes for directory: {directoryPath}");
            }
        }

        private void ProcessBufferedChanges(string directoryPath)
        {
            try
            {
                lock (_watchersLock)
                {
                    if (_watchers.TryGetValue(directoryPath, out var watcherInfo))
                    {
                        var changes = new List<FileSystemEventArgs>();
                        while (watcherInfo.ChangeBuffer.TryDequeue(out var args))
                        {
                            changes.Add(args);
                        }

                        if (changes.Any())
                        {
                            FileChangesBuffered?.Invoke(this, changes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing buffered changes for directory: {directoryPath}");
            }
        }

        private void ResetChangeCounters(object? state)
        {
            lock (_changeCounters)
            {
                _changeCounters.Clear();
            }
        }

        public async Task StopWatchingAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation($"Stopping watch on directory: {directoryPath}");

                lock (_watchersLock)
                {
                    if (_watchers.TryGetValue(directoryPath, out var watcherInfo))
                    {
                        watcherInfo.Watcher.EnableRaisingEvents = false;
                        watcherInfo.Watcher.Dispose();
                        watcherInfo.DebounceTimer?.Dispose();
                        watcherInfo.BufferTimer?.Dispose();
                        _watchers.Remove(directoryPath);
                    }
                }

                _logger.LogInformation($"Successfully stopped watching directory: {directoryPath}");
            }
            catch (Exception ex)
            {
                var errorId = await _errorHandler.LogErrorAsync(ex, $"FileService.StopWatchingAsync: {directoryPath}");
                _logger.LogError($"Error stopping directory watch. Error ID: {errorId}");
                throw;
            }
        }

        public void Dispose()
        {
            lock (_watchersLock)
            {
                foreach (var (watcher, _, debounceTimer, bufferTimer, _) in _watchers.Values)
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                    debounceTimer?.Dispose();
                    bufferTimer?.Dispose();
                }
                _watchers.Clear();
            }
            _changeCounterResetTimer.Dispose();
        }

        // Encrypt a file
/* var encryptedData = await fileService.EncryptFileAsync("sensitive.txt", "your-secure-key");

// Decrypt a file
await fileService.DecryptFileAsync(encryptedData, "decrypted.txt", "your-secure-key");

// Calculate and verify checksum
var checksum = await fileService.CalculateChecksumAsync("important.txt");
var isValid = await fileService.VerifyChecksumAsync("important.txt", checksum); */

        private class FileLock : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public FileLock(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
} 