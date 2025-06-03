using IntegrationPlatform.Api.Controllers;
using IntegrationPlatform.Contracts.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IntegrationPlatform.Api.Tests;

public class IntegrationControllerTests
{
    private readonly Mock<ILogger<IntegrationController>> _loggerMock;
    private readonly Mock<ISftpService> _sftpServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IMonitoringService> _monitoringServiceMock;
    private readonly IntegrationController _controller;

    public IntegrationControllerTests()
    {
        _loggerMock = new Mock<ILogger<IntegrationController>>();
        _sftpServiceMock = new Mock<ISftpService>();
        _emailServiceMock = new Mock<IEmailService>();
        _monitoringServiceMock = new Mock<IMonitoringService>();
        _controller = new IntegrationController(
            _loggerMock.Object,
            _sftpServiceMock.Object,
            _emailServiceMock.Object,
            _monitoringServiceMock.Object);
    }

    [Theory]
    [InlineData("test.sftp.com", 22, "testuser", "test.pfx", "test.txt", "/remote/test.txt")]
    [InlineData("sftp.example.com", 2222, "admin", "cert.pfx", "data.csv", "/remote/data.csv")]
    public async Task UploadFile_ValidRequest_ReturnsOkResult(string host, int port, string username, string certificatePath, string localPath, string remotePath)
    {
        // Arrange
        _sftpServiceMock.Setup(x => x.ConnectAsync(host, port, username, certificatePath))
            .ReturnsAsync(true);
        _sftpServiceMock.Setup(x => x.UploadFileAsync(localPath, remotePath))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UploadFile(host, port, username, certificatePath, localPath, remotePath);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True((bool)okResult.Value!);
        _sftpServiceMock.Verify(x => x.ConnectAsync(host, port, username, certificatePath), Times.Once);
        _sftpServiceMock.Verify(x => x.UploadFileAsync(localPath, remotePath), Times.Once);
    }

    [Theory]
    [InlineData("", 22, "testuser", "test.pfx", "test.txt", "/remote/test.txt", "host")]
    [InlineData("test.sftp.com", -1, "testuser", "test.pfx", "test.txt", "/remote/test.txt", "port")]
    [InlineData("test.sftp.com", 22, "", "test.pfx", "test.txt", "/remote/test.txt", "username")]
    [InlineData("test.sftp.com", 22, "testuser", "", "test.txt", "/remote/test.txt", "certificatePath")]
    [InlineData("test.sftp.com", 22, "testuser", "test.pfx", "", "/remote/test.txt", "localPath")]
    [InlineData("test.sftp.com", 22, "testuser", "test.pfx", "test.txt", "", "remotePath")]
    public async Task UploadFile_InvalidParameters_ReturnsBadRequest(string host, int port, string username, string certificatePath, string localPath, string remotePath, string paramName)
    {
        // Act
        var result = await _controller.UploadFile(host, port, username, certificatePath, localPath, remotePath);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(paramName, badRequestResult.Value!.ToString()!);
    }

    [Theory]
    [InlineData("test.sftp.com", 22, "testuser", "test.pfx", "test.txt", "/remote/test.txt")]
    [InlineData("sftp.example.com", 2222, "admin", "cert.pfx", "data.csv", "/remote/data.csv")]
    public async Task DownloadFile_ValidRequest_ReturnsOkResult(string host, int port, string username, string certificatePath, string remotePath, string localPath)
    {
        // Arrange
        _sftpServiceMock.Setup(x => x.ConnectAsync(host, port, username, certificatePath))
            .ReturnsAsync(true);
        _sftpServiceMock.Setup(x => x.DownloadFileAsync(remotePath, localPath))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DownloadFile(host, port, username, certificatePath, remotePath, localPath);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True((bool)okResult.Value!);
        _sftpServiceMock.Verify(x => x.ConnectAsync(host, port, username, certificatePath), Times.Once);
        _sftpServiceMock.Verify(x => x.DownloadFileAsync(remotePath, localPath), Times.Once);
    }

    [Theory]
    [InlineData("", 22, "testuser", "test.pfx", "/remote/test.txt", "downloaded.txt", "host")]
    [InlineData("test.sftp.com", -1, "testuser", "test.pfx", "/remote/test.txt", "downloaded.txt", "port")]
    [InlineData("test.sftp.com", 22, "", "test.pfx", "/remote/test.txt", "downloaded.txt", "username")]
    [InlineData("test.sftp.com", 22, "testuser", "", "/remote/test.txt", "downloaded.txt", "certificatePath")]
    [InlineData("test.sftp.com", 22, "testuser", "test.pfx", "", "downloaded.txt", "remotePath")]
    [InlineData("test.sftp.com", 22, "testuser", "test.pfx", "/remote/test.txt", "", "localPath")]
    public async Task DownloadFile_InvalidParameters_ReturnsBadRequest(string host, int port, string username, string certificatePath, string remotePath, string localPath, string paramName)
    {
        // Act
        var result = await _controller.DownloadFile(host, port, username, certificatePath, remotePath, localPath);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(paramName, badRequestResult.Value!.ToString()!);
    }

    [Theory]
    [InlineData("recipient@example.com", "Test Subject", "Test Body", false)]
    [InlineData("user@domain.com", "Another Subject", "Another Body", true)]
    public async Task SendEmail_ValidRequest_ReturnsOkResult(string to, string subject, string body, bool isHtml)
    {
        // Arrange
        var attachments = new List<string>();
        _emailServiceMock.Setup(x => x.SendEmailAsync(to, subject, body, isHtml, attachments))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SendEmail(to, subject, body, isHtml, attachments);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True((bool)okResult.Value!);
        _emailServiceMock.Verify(x => x.SendEmailAsync(to, subject, body, isHtml, attachments), Times.Once);
    }

    [Theory]
    [InlineData("", "Test Subject", "Test Body", false, "to")]
    [InlineData("recipient@example.com", "", "Test Body", false, "subject")]
    [InlineData("recipient@example.com", "Test Subject", "", false, "body")]
    public async Task SendEmail_InvalidParameters_ReturnsBadRequest(string to, string subject, string body, bool isHtml, string paramName)
    {
        // Arrange
        var attachments = new List<string>();

        // Act
        var result = await _controller.SendEmail(to, subject, body, isHtml, attachments);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains(paramName, badRequestResult.Value!.ToString()!);
    }

    [Theory]
    [InlineData("SFTP_UPLOAD")]
    [InlineData("EMAIL_SEND")]
    [InlineData("SFTP_DOWNLOAD")]
    public async Task GetOperationHistory_ValidRequest_ReturnsOkResult(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;
        var expectedHistory = new List<object>();
        _monitoringServiceMock.Setup(x => x.GetOperationHistoryAsync(operationType, startDate, endDate))
            .ReturnsAsync(expectedHistory);

        // Act
        var result = await _controller.GetOperationHistory(operationType, startDate, endDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expectedHistory, okResult.Value);
        _monitoringServiceMock.Verify(x => x.GetOperationHistoryAsync(operationType, startDate, endDate), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetOperationHistory_InvalidOperationType_ReturnsBadRequest(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetOperationHistory(operationType, startDate, endDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetOperationHistory_InvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var operationType = "SFTP_UPLOAD";
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-1); // End date before start date

        // Act
        var result = await _controller.GetOperationHistory(operationType, startDate, endDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Theory]
    [InlineData("SFTP_UPLOAD")]
    [InlineData("EMAIL_SEND")]
    [InlineData("SFTP_DOWNLOAD")]
    public async Task GetOperationStatistics_ValidRequest_ReturnsOkResult(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;
        var expectedStats = new Dictionary<string, object>();
        _monitoringServiceMock.Setup(x => x.GetOperationStatisticsAsync(operationType, startDate, endDate))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetOperationStatistics(operationType, startDate, endDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expectedStats, okResult.Value);
        _monitoringServiceMock.Verify(x => x.GetOperationStatisticsAsync(operationType, startDate, endDate), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetOperationStatistics_InvalidOperationType_ReturnsBadRequest(string operationType)
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-1);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetOperationStatistics(operationType, startDate, endDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetOperationStatistics_InvalidDateRange_ReturnsBadRequest()
    {
        // Arrange
        var operationType = "SFTP_UPLOAD";
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-1); // End date before start date

        // Act
        var result = await _controller.GetOperationStatistics(operationType, startDate, endDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetOperationStatistics_FutureDateRange_ReturnsBadRequest()
    {
        // Arrange
        var operationType = "SFTP_UPLOAD";
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(2);

        // Act
        var result = await _controller.GetOperationStatistics(operationType, startDate, endDate);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
} 