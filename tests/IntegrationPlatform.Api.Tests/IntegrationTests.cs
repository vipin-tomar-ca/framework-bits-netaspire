using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Email.Services;
using IntegrationPlatform.SFTP.Services;
using IntegrationPlatform.Monitoring.Services;
using IntegrationPlatform.Infrastructure.Services;
using IntegrationPlatform.Core.Services;
using Xunit;

namespace IntegrationPlatform.Api.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public IntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task TestEmailServiceIntegration()
        {
            // Arrange
            var client = _factory.CreateClient();
            var emailService = _factory.Services.GetRequiredService<IEmailService>();

            // Act
            var result = await emailService.ConnectAsync("smtp.example.com", 587, "user", "password");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TestSftpServiceIntegration()
        {
            // Arrange
            var client = _factory.CreateClient();
            var sftpService = _factory.Services.GetRequiredService<ISftpService>();

            // Act
            var result = await sftpService.ConnectAsync("sftp.example.com", 22, "user", "path/to/certificate.pfx", CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task TestMonitoringServiceIntegration()
        {
            // Arrange
            var client = _factory.CreateClient();
            var monitoringService = _factory.Services.GetRequiredService<IMonitoringService>();

            // Act
            var activity = await monitoringService.StartActivityAsync("TestActivity", "TestOperation");

            // Assert
            Assert.NotNull(activity);
        }
    }
} 