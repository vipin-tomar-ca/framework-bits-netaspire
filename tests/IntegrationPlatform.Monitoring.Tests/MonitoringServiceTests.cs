using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Monitoring.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IntegrationPlatform.Monitoring.Tests;

public class MonitoringServiceTests
{
    private readonly Mock<ILogger<MonitoringService>> _loggerMock;
    private readonly MonitoringService _monitoringService;

    public MonitoringServiceTests()
    {
        _loggerMock = new Mock<ILogger<MonitoringService>>();
        _monitoringService = new MonitoringService(_loggerMock.Object);
    }

    [Theory]
    [InlineData("SFTP_UPLOAD", "SUCCESS", "File uploaded successfully")]
    [InlineData("EMAIL_SEND", "SUCCESS", "Email sent successfully")]
    [InlineData("SFTP_DOWNLOAD", "SUCCESS", "File downloaded successfully")]
    public async Task LogOperationAsync_ValidOperation_LogsSuccessfully(string operationType, string status, string details)
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            { "fileName", "test.txt" },
            { "fileSize", "1024" }
        };

        // Act
        await _monitoringService.LogOperationAsync(operationType, status, details, metadata);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation logged")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("", "SUCCESS", "Details", "operationType")]
    [InlineData("SFTP_UPLOAD", "", "Details", "status")]
    [InlineData("SFTP_UPLOAD", "SUCCESS", "", "details")]
    public async Task LogOperationAsync_InvalidParameters_ThrowsArgumentException(string operationType, string status, string details, string paramName)
    {
        // Arrange
        var metadata = new Dictionary<string, string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _monitoringService.LogOperationAsync(operationType, status, details, metadata));
        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public async Task LogOperationAsync_ErrorStatus_LogsError()
    {
        // Arrange
        var operationType = "SFTP_UPLOAD";
        var status = "ERROR";
        var details = "Failed to upload file";
        var metadata = new Dictionary<string, string>
        {
            { "fileName", "test.txt" },
            { "error", "Connection timeout" }
        };

        // Act
        await _monitoringService.LogOperationAsync(operationType, status, details, metadata);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation logged")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("SFTP_UPLOAD")]
    [InlineData("EMAIL_SEND")]
    [InlineData("SFTP_DOWNLOAD")]
    public async Task GetOperationHistoryAsync_ValidParameters_ReturnsHistory(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _monitoringService.GetOperationHistoryAsync(operationType, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<List<object>>(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved operation history")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetOperationHistoryAsync_InvalidOperationType_ThrowsArgumentException(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _monitoringService.GetOperationHistoryAsync(operationType, startDate, endDate));
    }

    [Fact]
    public async Task GetOperationHistoryAsync_InvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var operationType = "SFTP_UPLOAD";
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-1); // End date before start date

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _monitoringService.GetOperationHistoryAsync(operationType, startDate, endDate));
    }

    [Theory]
    [InlineData("SFTP_UPLOAD")]
    [InlineData("EMAIL_SEND")]
    [InlineData("SFTP_DOWNLOAD")]
    public async Task GetOperationStatisticsAsync_ValidParameters_ReturnsStatistics(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _monitoringService.GetOperationStatisticsAsync(operationType, startDate, endDate);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Dictionary<string, object>>(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Retrieved operation statistics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetOperationStatisticsAsync_InvalidOperationType_ThrowsArgumentException(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _monitoringService.GetOperationStatisticsAsync(operationType, startDate, endDate));
    }

    [Fact]
    public async Task GetOperationStatisticsAsync_InvalidDateRange_ThrowsArgumentException()
    {
        // Arrange
        var operationType = "SFTP_UPLOAD";
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-1); // End date before start date

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _monitoringService.GetOperationStatisticsAsync(operationType, startDate, endDate));
    }

    [Fact]
    public async Task GetOperationStatisticsAsync_FutureDateRange_ThrowsArgumentException()
    {
        // Arrange
        var operationType = "SFTP_UPLOAD";
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(2);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _monitoringService.GetOperationStatisticsAsync(operationType, startDate, endDate));
    }
} 