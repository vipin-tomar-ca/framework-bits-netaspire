using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Email.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net.Mail;
using Xunit;

namespace IntegrationPlatform.Email.Tests;

public class EmailServiceTests
{
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _loggerMock = new Mock<ILogger<EmailService>>();
        _emailService = new EmailService(_loggerMock.Object);
    }

    [Theory]
    [InlineData("recipient@example.com", "Test Subject", "Test Body", false)]
    [InlineData("user@domain.com", "Another Subject", "Another Body", true)]
    public async Task SendEmailAsync_ValidEmail_SendsSuccessfully(string to, string subject, string body, bool isHtml)
    {
        // Arrange
        var attachments = new List<string>();

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully sent email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("", "Test Subject", "Test Body", false, "to")]
    [InlineData("recipient@example.com", "", "Test Body", false, "subject")]
    [InlineData("recipient@example.com", "Test Subject", "", false, "body")]
    public async Task SendEmailAsync_InvalidParameters_ThrowsArgumentException(string to, string subject, string body, bool isHtml, string paramName)
    {
        // Arrange
        var attachments = new List<string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
            _emailService.SendEmailAsync(to, subject, body, isHtml, attachments));
        Assert.Equal(paramName, exception.ParamName);
    }

    [Fact]
    public async Task SendEmailAsync_WithAttachments_SendsSuccessfully()
    {
        // Arrange
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";
        var isHtml = false;
        var attachments = new List<string> { "test.txt" };
        File.WriteAllText("test.txt", "test content");

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully sent email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        File.Delete("test.txt");
    }

    [Fact]
    public async Task SendEmailAsync_WithMultipleAttachments_SendsSuccessfully()
    {
        // Arrange
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";
        var isHtml = false;
        var attachments = new List<string> { "test1.txt", "test2.txt" };
        File.WriteAllText("test1.txt", "test content 1");
        File.WriteAllText("test2.txt", "test content 2");

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully sent email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        File.Delete("test1.txt");
        File.Delete("test2.txt");
    }

    [Fact]
    public async Task SendEmailAsync_HtmlEmail_SendsSuccessfully()
    {
        // Arrange
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var body = "<html><body>Test Body</body></html>";
        var isHtml = true;
        var attachments = new List<string>();

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully sent email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    [InlineData("user@domain")]
    public async Task SendEmailAsync_InvalidEmail_ReturnsFalse(string to)
    {
        // Arrange
        var subject = "Test Subject";
        var body = "Test Body";
        var isHtml = false;
        var attachments = new List<string>();

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_NonExistentAttachment_ReturnsFalse()
    {
        // Arrange
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";
        var isHtml = false;
        var attachments = new List<string> { "nonexistent.txt" };

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_AttachmentTooLarge_ReturnsFalse()
    {
        // Arrange
        var to = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";
        var isHtml = false;
        var attachments = new List<string> { "large.txt" };
        
        // Create a large file (e.g., 100MB)
        using (var file = File.Create("large.txt"))
        {
            file.SetLength(100 * 1024 * 1024); // 100MB
        }

        // Act
        var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to send email")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Cleanup
        File.Delete("large.txt");
    }
} 