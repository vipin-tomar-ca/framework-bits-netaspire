using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using IntegrationPlatform.Infrastructure.FileSystem;
using IntegrationPlatform.Infrastructure.ErrorHandling;

namespace IntegrationPlatform.Infrastructure.Tests.FileSystem
{
    public class FileServiceTests : IDisposable
    {
        private readonly Mock<ILogger<FileService>> _loggerMock;
        private readonly Mock<ErrorHandler> _errorHandlerMock;
        private readonly FileService _fileService;
        private readonly string _testDirectory;
        private readonly string _testFilePath;
        private const string TestContent = "Test content for file operations";
        private const string EncryptionKey = "TestEncryptionKey123!";

        public FileServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileService>>();
            _errorHandlerMock = new Mock<ErrorHandler>();
            _fileService = new FileService(_loggerMock.Object, _errorHandlerMock.Object);
            
            _testDirectory = Path.Combine(Path.GetTempPath(), "FileServiceTests");
            _testFilePath = Path.Combine(_testDirectory, "test.txt");
            
            Directory.CreateDirectory(_testDirectory);
            File.WriteAllText(_testFilePath, TestContent);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public async Task EncryptAndDecryptFile_ShouldWorkCorrectly()
        {
            // Arrange
            var encryptedFilePath = Path.Combine(_testDirectory, "encrypted.bin");
            var decryptedFilePath = Path.Combine(_testDirectory, "decrypted.txt");

            // Act
            var encryptedData = await _fileService.EncryptFileAsync(_testFilePath, EncryptionKey);
            await File.WriteAllBytesAsync(encryptedFilePath, encryptedData);
            await _fileService.DecryptFileAsync(encryptedData, decryptedFilePath, EncryptionKey);

            // Assert
            var decryptedContent = await File.ReadAllTextAsync(decryptedFilePath);
            Assert.Equal(TestContent, decryptedContent);
        }

        [Fact]
        public async Task EncryptFile_WithInvalidKey_ShouldThrowException()
        {
            // Arrange
            var invalidKey = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _fileService.EncryptFileAsync(_testFilePath, invalidKey));
        }

        [Fact]
        public async Task DecryptFile_WithInvalidKey_ShouldThrowException()
        {
            // Arrange
            var encryptedData = await _fileService.EncryptFileAsync(_testFilePath, EncryptionKey);
            var decryptedFilePath = Path.Combine(_testDirectory, "decrypted.txt");
            var invalidKey = "WrongKey123!";

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(() => 
                _fileService.DecryptFileAsync(encryptedData, decryptedFilePath, invalidKey));
        }

        [Fact]
        public async Task CalculateAndVerifyChecksum_ShouldWorkCorrectly()
        {
            // Act
            var checksum = await _fileService.CalculateChecksumAsync(_testFilePath);
            var isValid = await _fileService.VerifyChecksumAsync(_testFilePath, checksum);

            // Assert
            Assert.True(isValid);
            Assert.NotNull(checksum);
            Assert.Equal(64, checksum.Length); // SHA-256 produces 64 characters in hex
        }

        [Fact]
        public async Task VerifyChecksum_WithInvalidChecksum_ShouldReturnFalse()
        {
            // Arrange
            var invalidChecksum = new string('0', 64);

            // Act
            var isValid = await _fileService.VerifyChecksumAsync(_testFilePath, invalidChecksum);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task CalculateChecksum_WithNonExistentFile_ShouldThrowException()
        {
            // Arrange
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() => 
                _fileService.CalculateChecksumAsync(nonExistentFile));
        }

        [Fact]
        public async Task FileOperations_WithCancellation_ShouldCancel()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => 
                _fileService.EncryptFileAsync(_testFilePath, EncryptionKey, cts.Token));
            await Assert.ThrowsAsync<TaskCanceledException>(() => 
                _fileService.CalculateChecksumAsync(_testFilePath, cts.Token));
        }

        [Fact]
        public async Task StartWatching_WithValidDirectory_ShouldStartWatcher()
        {
            // Act
            await _fileService.StartWatchingAsync(_testDirectory);

            // Assert
            // No exception should be thrown
        }

        [Fact]
        public async Task StartWatching_WithInvalidDirectory_ShouldThrowException()
        {
            // Arrange
            var invalidDirectory = Path.Combine(_testDirectory, "nonexistent");

            // Act & Assert
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => 
                _fileService.StartWatchingAsync(invalidDirectory));
        }

        [Fact]
        public async Task StopWatching_ShouldStopWatcher()
        {
            // Arrange
            await _fileService.StartWatchingAsync(_testDirectory);

            // Act
            await _fileService.StopWatchingAsync(_testDirectory);

            // Assert
            // No exception should be thrown
        }

        [Fact]
        public async Task FileChanged_Event_ShouldBeRaised()
        {
            // Arrange
            var eventRaised = false;
            var changeType = FileSystemChangeType.Created;
            var changedFilePath = "";

            _fileService.FileChanged += (s, e) =>
            {
                eventRaised = true;
                changeType = e.ChangeType;
                changedFilePath = e.FilePath;
            };

            await _fileService.StartWatchingAsync(_testDirectory);

            // Act
            var newFilePath = Path.Combine(_testDirectory, "newfile.txt");
            await File.WriteAllTextAsync(newFilePath, "test content");

            // Wait for the event to be raised
            await Task.Delay(100);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(FileSystemChangeType.Created, changeType);
            Assert.Equal(newFilePath, changedFilePath);
        }

        [Fact]
        public async Task FileChanged_Event_ShouldHandleMultipleChanges()
        {
            // Arrange
            var changeCount = 0;
            var expectedChanges = new[] { FileSystemChangeType.Created, FileSystemChangeType.Modified, FileSystemChangeType.Deleted };

            _fileService.FileChanged += (s, e) =>
            {
                changeCount++;
            };

            await _fileService.StartWatchingAsync(_testDirectory);

            // Act
            var testFilePath = Path.Combine(_testDirectory, "testfile.txt");
            await File.WriteAllTextAsync(testFilePath, "test content"); // Created
            await File.WriteAllTextAsync(testFilePath, "modified content"); // Modified
            File.Delete(testFilePath); // Deleted

            // Wait for all events to be raised
            await Task.Delay(100);

            // Assert
            Assert.Equal(3, changeCount);
        }

        [Fact]
        public async Task FileChanged_Event_ShouldHandleSubdirectories()
        {
            // Arrange
            var subdirectory = Path.Combine(_testDirectory, "subdir");
            Directory.CreateDirectory(subdirectory);

            var eventRaised = false;
            var changedFilePath = "";

            _fileService.FileChanged += (s, e) =>
            {
                eventRaised = true;
                changedFilePath = e.FilePath;
            };

            await _fileService.StartWatchingAsync(_testDirectory);

            // Act
            var newFilePath = Path.Combine(subdirectory, "subfile.txt");
            await File.WriteAllTextAsync(newFilePath, "test content");

            // Wait for the event to be raised
            await Task.Delay(100);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(newFilePath, changedFilePath);
        }

        [Fact]
        public async Task StartWatching_WithCancellation_ShouldCancel()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() => 
                _fileService.StartWatchingAsync(_testDirectory, cts.Token));
        }

        [Fact]
        public async Task StartWatching_WithFileExtensions_ShouldFilterChanges()
        {
            // Arrange
            var options = new FileWatchOptions
            {
                FileExtensions = new[] { ".txt" }
            };

            var eventRaised = false;
            var changedFilePath = "";

            _fileService.FileChanged += (s, e) =>
            {
                eventRaised = true;
                changedFilePath = e.FilePath;
            };

            await _fileService.StartWatchingAsync(_testDirectory, options);

            // Act
            var txtFilePath = Path.Combine(_testDirectory, "test.txt");
            var pdfFilePath = Path.Combine(_testDirectory, "test.pdf");
            await File.WriteAllTextAsync(txtFilePath, "test content");
            await File.WriteAllTextAsync(pdfFilePath, "test content");

            // Wait for events
            await Task.Delay(100);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(txtFilePath, changedFilePath);
        }

        [Fact]
        public async Task StartWatching_WithFileSizeLimits_ShouldFilterChanges()
        {
            // Arrange
            var options = new FileWatchOptions
            {
                MinFileSize = 10,
                MaxFileSize = 100
            };

            var eventRaised = false;
            var changedFilePath = "";

            _fileService.FileChanged += (s, e) =>
            {
                eventRaised = true;
                changedFilePath = e.FilePath;
            };

            await _fileService.StartWatchingAsync(_testDirectory, options);

            // Act
            var smallFilePath = Path.Combine(_testDirectory, "small.txt");
            var mediumFilePath = Path.Combine(_testDirectory, "medium.txt");
            var largeFilePath = Path.Combine(_testDirectory, "large.txt");
            await File.WriteAllTextAsync(smallFilePath, "small");
            await File.WriteAllTextAsync(mediumFilePath, new string('x', 50));
            await File.WriteAllTextAsync(largeFilePath, new string('x', 200));

            // Wait for events
            await Task.Delay(100);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(mediumFilePath, changedFilePath);
        }

        [Fact]
        public async Task StartWatching_WithDebounce_ShouldDelayEvents()
        {
            // Arrange
            var options = new FileWatchOptions
            {
                DebounceInterval = TimeSpan.FromMilliseconds(100)
            };

            var changes = new List<string>();
            _fileService.FileChanged += (s, e) => changes.Add(e.FilePath);

            await _fileService.StartWatchingAsync(_testDirectory, options);

            // Act
            var filePath = Path.Combine(_testDirectory, "test.txt");
            await File.WriteAllTextAsync(filePath, "content1");
            await File.WriteAllTextAsync(filePath, "content2");
            await File.WriteAllTextAsync(filePath, "content3");

            // Wait for debounce
            await Task.Delay(200);

            // Assert
            Assert.Single(changes);
            Assert.Equal(filePath, changes[0]);
        }

        [Fact]
        public async Task StartWatching_WithBuffer_ShouldBatchEvents()
        {
            // Arrange
            var options = new FileWatchOptions
            {
                BufferInterval = TimeSpan.FromMilliseconds(100)
            };

            var bufferedChanges = new List<IEnumerable<FileSystemEventArgs>>();
            _fileService.FileChangesBuffered += (s, e) => bufferedChanges.Add(e);

            await _fileService.StartWatchingAsync(_testDirectory, options);

            // Act
            var file1 = Path.Combine(_testDirectory, "test1.txt");
            var file2 = Path.Combine(_testDirectory, "test2.txt");
            var file3 = Path.Combine(_testDirectory, "test3.txt");
            await File.WriteAllTextAsync(file1, "content1");
            await File.WriteAllTextAsync(file2, "content2");
            await File.WriteAllTextAsync(file3, "content3");

            // Wait for buffer
            await Task.Delay(200);

            // Assert
            Assert.Single(bufferedChanges);
            var changes = bufferedChanges[0].ToList();
            Assert.Equal(3, changes.Count);
            Assert.Contains(changes, c => c.FilePath == file1);
            Assert.Contains(changes, c => c.FilePath == file2);
            Assert.Contains(changes, c => c.FilePath == file3);
        }

        [Fact]
        public async Task StartWatching_WithChangeRateLimit_ShouldLimitEvents()
        {
            // Arrange
            var options = new FileWatchOptions
            {
                MaxChangesPerMinute = 2
            };

            var changes = new List<string>();
            _fileService.FileChanged += (s, e) => changes.Add(e.FilePath);

            await _fileService.StartWatchingAsync(_testDirectory, options);

            // Act
            var file1 = Path.Combine(_testDirectory, "test1.txt");
            var file2 = Path.Combine(_testDirectory, "test2.txt");
            var file3 = Path.Combine(_testDirectory, "test3.txt");
            await File.WriteAllTextAsync(file1, "content1");
            await File.WriteAllTextAsync(file2, "content2");
            await File.WriteAllTextAsync(file3, "content3");

            // Wait for events
            await Task.Delay(100);

            // Assert
            Assert.Equal(2, changes.Count);
            Assert.Contains(file1, changes);
            Assert.Contains(file2, changes);
            Assert.DoesNotContain(file3, changes);
        }

        [Fact]
        public async Task StartWatching_WithSubdirectories_ShouldWatchNestedFolders()
        {
            // Arrange
            var options = new FileWatchOptions
            {
                IncludeSubdirectories = true
            };

            var eventRaised = false;
            var changedFilePath = "";

            _fileService.FileChanged += (s, e) =>
            {
                eventRaised = true;
                changedFilePath = e.FilePath;
            };

            await _fileService.StartWatchingAsync(_testDirectory, options);

            // Act
            var subdir = Path.Combine(_testDirectory, "subdir");
            Directory.CreateDirectory(subdir);
            var filePath = Path.Combine(subdir, "test.txt");
            await File.WriteAllTextAsync(filePath, "test content");

            // Wait for events
            await Task.Delay(100);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(filePath, changedFilePath);
        }

        [Fact]
        public async Task StartWatching_WithoutSubdirectories_ShouldNotWatchNestedFolders()
        {
            // Arrange
            var options = new FileWatchOptions
            {
                IncludeSubdirectories = false
            };

            var eventRaised = false;
            _fileService.FileChanged += (s, e) => eventRaised = true;

            await _fileService.StartWatchingAsync(_testDirectory, options);

            // Act
            var subdir = Path.Combine(_testDirectory, "subdir");
            Directory.CreateDirectory(subdir);
            var filePath = Path.Combine(subdir, "test.txt");
            await File.WriteAllTextAsync(filePath, "test content");

            // Wait for events
            await Task.Delay(100);

            // Assert
            Assert.False(eventRaised);
        }
    }
} 