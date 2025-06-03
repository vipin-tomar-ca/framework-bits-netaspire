using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.SFTP.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using Xunit;

namespace IntegrationPlatform.SFTP.Tests;

public class SftpServiceTests
{
    private readonly Mock<ILogger<SftpService>> _loggerMock;
    private readonly SftpService _sftpService;

    public SftpServiceTests()
    {
        _loggerMock = new Mock<ILogger<SftpService>>();
        _sftpService = new SftpService(_loggerMock.Object);
    }

    [Theory]
    [InlineData("test.sftp.com", 22, "testuser", "test.pfx")]
    [InlineData("sftp.example.com", 2222, "admin", "cert.pfx")]
    public async Task ConnectAsync_ValidCredentials_ReturnsTrue(string host, int port, string username, string certificatePath)
    {
        // Act
        var result = await _sftpService.ConnectAsync(host, port, username, certificatePath);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("", 22, "testuser", "test.pfx", "host")]
    [InlineData("test.sftp.com", -1, "testuser", "test.pfx", "port")]
    [InlineData("test.sftp.com", 22, "", "test.pfx", "username")]
    [InlineData("test.sftp.com", 22, "testuser", "", "certificatePath")]
    public async Task ConnectAsync_InvalidParameters_ThrowsArgumentException(string host, int port, string username, string certificatePath, string paramName)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _sftpService.ConnectAsync(host, port, username, certificatePath));
        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public async Task ConnectAsync_NonExistentCertificate_ThrowsFileNotFoundException()
    {
        // Arrange
        var host = "test.sftp.com";
        var port = 22;
        var username = "testuser";
        var certificatePath = "nonexistent.pfx";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _sftpService.ConnectAsync(host, port, username, certificatePath));
    }

    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var localPath = "test.txt";
        var remotePath = "/remote/test.txt";
        File.WriteAllText(localPath, "test content");

        // Act
        var result = await _sftpService.UploadFileAsync(localPath, remotePath);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully uploaded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        File.Delete(localPath);
    }

    [Theory]
    [InlineData("", "/remote/test.txt", "localPath")]
    [InlineData("test.txt", "", "remotePath")]
    public async Task UploadFileAsync_InvalidParameters_ThrowsArgumentException(string localPath, string remotePath, string paramName)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _sftpService.UploadFileAsync(localPath, remotePath));
        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public async Task UploadFileAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var localPath = "nonexistent.txt";
        var remotePath = "/remote/test.txt";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => 
            _sftpService.UploadFileAsync(localPath, remotePath));
    }

    [Fact]
    public async Task DownloadFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var remotePath = "/remote/test.txt";
        var localPath = "downloaded.txt";

        // Act
        var result = await _sftpService.DownloadFileAsync(remotePath, localPath);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully downloaded")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }
    }

    [Theory]
    [InlineData("", "downloaded.txt", "remotePath")]
    [InlineData("/remote/test.txt", "", "localPath")]
    public async Task DownloadFileAsync_InvalidParameters_ThrowsArgumentException(string remotePath, string localPath, string paramName)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _sftpService.DownloadFileAsync(remotePath, localPath));
        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public async Task DeleteFileAsync_ValidFile_ReturnsTrue()
    {
        // Arrange
        var remotePath = "/remote/test.txt";

        // Act
        var result = await _sftpService.DeleteFileAsync(remotePath);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully deleted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task DeleteFileAsync_InvalidPath_ThrowsArgumentException(string remotePath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sftpService.DeleteFileAsync(remotePath));
    }

    [Fact]
    public async Task ListFilesAsync_ValidDirectory_ReturnsFileList()
    {
        // Arrange
        var remotePath = "/remote";

        // Act
        var result = await _sftpService.ListFilesAsync(remotePath);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<string>>(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully listed files")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ListFilesAsync_InvalidPath_ThrowsArgumentException(string remotePath)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _sftpService.ListFilesAsync(remotePath));
    }

    [Fact]
    public async Task DisconnectAsync_ConnectedClient_DisconnectsSuccessfully()
    {
        // Act
        await _sftpService.DisconnectAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully disconnected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_NotConnected_LogsWarning()
    {
        // Act
        await _sftpService.DisconnectAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Not connected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
} 