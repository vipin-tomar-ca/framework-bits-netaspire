using IntegrationPlatform.Contracts.Attributes;
using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace IntegrationPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AuditTrail("Integration", "API")]
public class IntegrationController : ControllerBase
{
    private readonly ILogger<IntegrationController> _logger;
    private readonly ISftpService _sftpService;
    private readonly IEmailService _emailService;
    private readonly IMonitoringService _monitoringService;

    public IntegrationController(
        ILogger<IntegrationController> logger,
        ISftpService sftpService,
        IEmailService emailService,
        IMonitoringService monitoringService)
    {
        _logger = logger;
        _sftpService = sftpService;
        _emailService = emailService;
        _monitoringService = monitoringService;
    }

    [HttpPost("sftp/connect")]
    [AuditTrail("SFTP Connect", "SFTP", LogRequestBody = false, SensitiveParameters = new[] { "certificatePath" })]
    public async Task<IActionResult> ConnectSftp(string host, int port, string username, string certificatePath, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(host)) return BadRequest("Host is required");
            if (port <= 0) return BadRequest("Port must be greater than 0");
            if (string.IsNullOrEmpty(username)) return BadRequest("Username is required");
            if (string.IsNullOrEmpty(certificatePath)) return BadRequest("Certificate path is required");

            var result = await _sftpService.ConnectAsync(host, port, username, certificatePath, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SFTP server");
            return StatusCode(500, "Failed to connect to SFTP server");
        }
    }

    [HttpPost("sftp/upload")]
    [AuditTrail("SFTP Upload", "SFTP", LogRequestBody = false)]
    public async Task<IActionResult> UploadFile(string localPath, string remotePath, CancellationToken cancellationToken)
    {
        try
        {
            if (!_sftpService.IsConnected)
                return BadRequest("Not connected to SFTP server. Please connect first.");

            if (string.IsNullOrEmpty(localPath)) return BadRequest("Local path is required");
            if (string.IsNullOrEmpty(remotePath)) return BadRequest("Remote path is required");

            var result = await _sftpService.UploadFileAsync(localPath, remotePath, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file");
            return StatusCode(500, "Failed to upload file");
        }
    }

    [HttpPost("sftp/download")]
    [AuditTrail("SFTP Download", "SFTP", LogRequestBody = false)]
    public async Task<IActionResult> DownloadFile(string remotePath, string localPath, CancellationToken cancellationToken)
    {
        try
        {
            if (!_sftpService.IsConnected)
                return BadRequest("Not connected to SFTP server. Please connect first.");

            if (string.IsNullOrEmpty(remotePath)) return BadRequest("Remote path is required");
            if (string.IsNullOrEmpty(localPath)) return BadRequest("Local path is required");

            var result = await _sftpService.DownloadFileAsync(remotePath, localPath, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file");
            return StatusCode(500, "Failed to download file");
        }
    }

    [HttpPost("sftp/delete")]
    [AuditTrail("SFTP Delete", "SFTP", LogRequestBody = false)]
    public async Task<IActionResult> DeleteFile(string remotePath, CancellationToken cancellationToken)
    {
        try
        {
            if (!_sftpService.IsConnected)
                return BadRequest("Not connected to SFTP server. Please connect first.");

            if (string.IsNullOrEmpty(remotePath)) return BadRequest("Remote path is required");

            var result = await _sftpService.DeleteFileAsync(remotePath, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file");
            return StatusCode(500, "Failed to delete file");
        }
    }

    [HttpGet("sftp/list")]
    [AuditTrail("SFTP List", "SFTP", LogRequestBody = false)]
    public async Task<IActionResult> ListFiles(string remotePath, CancellationToken cancellationToken)
    {
        try
        {
            if (!_sftpService.IsConnected)
                return BadRequest("Not connected to SFTP server. Please connect first.");

            if (string.IsNullOrEmpty(remotePath)) return BadRequest("Remote path is required");

            var result = await _sftpService.ListFilesAsync(remotePath, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list files");
            return StatusCode(500, "Failed to list files");
        }
    }

    [HttpPost("sftp/disconnect")]
    [AuditTrail("SFTP Disconnect", "SFTP", LogRequestBody = false)]
    public async Task<IActionResult> DisconnectSftp(CancellationToken cancellationToken)
    {
        try
        {
            await _sftpService.DisconnectAsync(cancellationToken);
            return Ok();
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disconnect from SFTP server");
            return StatusCode(500, "Failed to disconnect from SFTP server");
        }
    }

    [HttpPost("email/send")]
    [AuditTrail("Email Send", "Email", SensitiveParameters = new[] { "password" })]
    public async Task<IActionResult> SendEmail(string to, string subject, string body, bool isHtml = false, List<string>? attachments = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(to)) return BadRequest("Recipient email is required");
            if (string.IsNullOrEmpty(subject)) return BadRequest("Subject is required");
            if (string.IsNullOrEmpty(body)) return BadRequest("Body is required");

            var result = await _emailService.SendEmailAsync(to, subject, body, isHtml, attachments ?? new List<string>(), cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            return StatusCode(500, "Failed to send email");
        }
    }

    [HttpGet("monitoring/history")]
    [AuditTrail("Monitoring History", "Monitoring", LogRequestBody = false)]
    public async Task<IActionResult> GetOperationHistory(string operationType, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(operationType)) return BadRequest("Operation type is required");
            if (startDate > endDate) return BadRequest("Start date must be before end date");
            if (startDate > DateTime.UtcNow) return BadRequest("Start date cannot be in the future");

            var result = await _monitoringService.GetOperationHistoryAsync(operationType, startDate, endDate, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation history");
            return StatusCode(500, "Failed to get operation history");
        }
    }

    [HttpGet("monitoring/statistics")]
    [AuditTrail("Monitoring Statistics", "Monitoring", LogRequestBody = false)]
    public async Task<IActionResult> GetOperationStatistics(string operationType, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(operationType)) return BadRequest("Operation type is required");
            if (startDate > endDate) return BadRequest("Start date must be before end date");
            if (startDate > DateTime.UtcNow) return BadRequest("Start date cannot be in the future");

            var result = await _monitoringService.GetOperationStatisticsAsync(operationType, startDate, endDate, cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(499, "Operation was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get operation statistics");
            return StatusCode(500, "Failed to get operation statistics");
        }
    }
}

public class SftpConnectionRequest
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string CertificatePath { get; set; } = string.Empty;
}

public class EmailConnectionRequest
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class FileTransferRequest
{
    public string LocalPath { get; set; } = string.Empty;
    public string RemotePath { get; set; } = string.Empty;
} 