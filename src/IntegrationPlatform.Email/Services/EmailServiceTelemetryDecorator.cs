using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using IntegrationPlatform.Contracts.Interfaces;
using IntegrationPlatform.Monitoring.Services;

namespace IntegrationPlatform.Email.Services;

/// <summary>
/// Decorator that adds telemetry spans & logs to <see cref="IEmailService"/>.
/// Register via DI similarly to other decorators.
/// </summary>
public sealed class EmailServiceTelemetryDecorator : IEmailService
{
    private readonly IEmailService _inner;
    private readonly Tracer _tracer;
    private readonly ILogger<EmailServiceTelemetryDecorator> _logger;

    public EmailServiceTelemetryDecorator(IEmailService inner, ILogger<EmailServiceTelemetryDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
        _tracer = TracerProvider.Default.GetTracer("IntegrationPlatform.Email");
    }

    public Task<bool> ConnectAsync(string host, int port, string username, string password) =>
        _tracer.TrackAsync(_logger, "EmailConnect", nameof(ConnectAsync), () => _inner.ConnectAsync(host, port, username, password), ("host", host));

    public Task<bool> SendEmailAsync(string to, string subject, string body, List<string>? attachments = null) =>
        _tracer.TrackAsync(_logger, "EmailSend", nameof(SendEmailAsync), () => _inner.SendEmailAsync(to, subject, body, attachments), ("recipient", to));

    public Task<List<EmailMessage>> GetUnreadEmailsAsync() =>
        _tracer.TrackAsync(_logger, "EmailUnread", nameof(GetUnreadEmailsAsync), () => _inner.GetUnreadEmailsAsync());

    public Task<List<EmailMessage>> RetrieveEmailsAsync(EmailRetrievalOptions options, CancellationToken cancellationToken = default) =>
        _tracer.TrackAsync(_logger, "EmailRetrieve", nameof(RetrieveEmailsAsync), () => _inner.RetrieveEmailsAsync(options, cancellationToken), ("protocol", options.Protocol));

    public Task<bool> MarkEmailAsReadAsync(string messageId) =>
        _tracer.TrackAsync(_logger, "EmailMarkRead", nameof(MarkEmailAsReadAsync), () => _inner.MarkEmailAsReadAsync(messageId), ("msgId", messageId));

    public Task<bool> DownloadAttachmentAsync(string messageId, string attachmentId, string localPath) =>
        _tracer.TrackAsync(_logger, "EmailAttachment", nameof(DownloadAttachmentAsync), () => _inner.DownloadAttachmentAsync(messageId, attachmentId, localPath), ("msgId", messageId));

    public Task DisconnectAsync() =>
        _tracer.TrackAsync(_logger, "EmailDisconnect", nameof(DisconnectAsync), () => _inner.DisconnectAsync());
}
