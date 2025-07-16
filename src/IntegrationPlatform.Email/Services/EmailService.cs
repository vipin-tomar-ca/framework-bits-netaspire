using IntegrationPlatform.Contracts.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Net.Pop3;
using HtmlAgilityPack;
using MailKit.Search;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Email.Services;

public class EmailService : IEmailService, IDisposable
{
    private readonly ILogger<EmailService> _logger;
    private ImapClient? _imapClient;
    private Pop3Client? _popClient;
    private SmtpClient? _smtpClient;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(string host, int port, string username, string password)
    {
        try
        {
            // Connect to IMAP server
            _imapClient = new ImapClient();
            await _imapClient.ConnectAsync(host, port, true);
            await _imapClient.AuthenticateAsync(username, password);

            // Connect to SMTP server
            _smtpClient = new SmtpClient();
            await _smtpClient.ConnectAsync(host, port, true);
            await _smtpClient.AuthenticateAsync(username, password);

            _logger.LogInformation("Successfully connected to email server at {Host}:{Port}", host, port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to email server at {Host}:{Port}", host, port);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, List<string>? attachments = null)
    {
        try
        {
            if (_smtpClient == null || !_smtpClient.IsConnected)
                throw new InvalidOperationException("SMTP client is not connected");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Integration Platform", "noreply@integrationplatform.com"));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;

            var builder = new BodyBuilder { TextBody = body };

            if (attachments != null)
            {
                foreach (var attachment in attachments)
                {
                    if (File.Exists(attachment))
                    {
                        builder.Attachments.Add(attachment);
                    }
                }
            }

            message.Body = builder.ToMessageBody();

            await _smtpClient.SendAsync(message);
            
            _logger.LogInformation("Successfully sent email to {To} with subject {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
            return false;
        }
    }

    public async Task<List<EmailMessage>> GetUnreadEmailsAsync()
    {
        try
        {
            if (_imapClient == null || !_imapClient.IsConnected)
                throw new InvalidOperationException("IMAP client is not connected");

            var inbox = _imapClient.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var uids = await inbox.SearchAsync(SearchQuery.NotSeen);
            var messages = new List<EmailMessage>();

            foreach (var uid in uids)
            {
                var message = await inbox.GetMessageAsync(uid);
                var emailMessage = new EmailMessage
                {
                    MessageId = message.MessageId,
                    From = message.From.ToString(),
                    Subject = message.Subject,
                    Body = message.TextBody,
                    ReceivedDate = message.Date.DateTime,
                    Attachments = message.Attachments.Select(a => new EmailAttachment
                    {
                        AttachmentId = a.ContentId,
                        FileName = a is MimePart mimePart ? mimePart.FileName : "unknown",
                        Size = a is MimePart mimePart ? mimePart.Size : 0,
                        ContentType = a.ContentType.MimeType
                    }).ToList()
                };

                messages.Add(emailMessage);
            }

            _logger.LogInformation("Successfully retrieved {Count} unread emails", messages.Count);
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve unread emails");
            return new List<EmailMessage>();
        }
    }

    public async Task<bool> MarkEmailAsReadAsync(string messageId)
    {
        try
        {
            if (_imapClient == null || !_imapClient.IsConnected)
                throw new InvalidOperationException("IMAP client is not connected");

            var inbox = _imapClient.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var uids = await inbox.SearchAsync(SearchQuery.HeaderContains("Message-Id", messageId));
            if (uids.Any())
            {
                await inbox.AddFlagsAsync(uids[0], MessageFlags.Seen, true);
                _logger.LogInformation("Successfully marked email {MessageId} as read", messageId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark email {MessageId} as read", messageId);
            return false;
        }
    }

    public async Task<bool> DownloadAttachmentAsync(string messageId, string attachmentId, string localPath)
    {
        try
        {
            if (_imapClient == null || !_imapClient.IsConnected)
                throw new InvalidOperationException("IMAP client is not connected");

            var inbox = _imapClient.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadOnly);

            var uids = await inbox.SearchAsync(SearchQuery.HeaderContains("Message-Id", messageId));
            if (!uids.Any())
                return false;

            var message = await inbox.GetMessageAsync(uids[0]);
            var attachment = message.Attachments.FirstOrDefault(a => a.ContentId == attachmentId);
            
            if (attachment == null)
                return false;

            using var stream = File.Create(localPath);
            if (attachment is MimePart mimePart)
            {
                await mimePart.Content.DecodeToAsync(stream);
            }
            
            _logger.LogInformation("Successfully downloaded attachment {AttachmentId} from email {MessageId}", attachmentId, messageId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download attachment {AttachmentId} from email {MessageId}", attachmentId, messageId);
            return false;
        }
    }

    public async Task<List<EmailMessage>> RetrieveEmailsAsync(EmailRetrievalOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            if (options.Protocol == EmailProtocol.Imap)
            {
                using var client = new ImapClient();
                await client.ConnectAsync(options.Host, options.Port, options.UseSsl, cancellationToken);
                await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);

                var inbox = client.GetFolder(options.Folder);
                await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

                var query = options.UnreadOnly ? SearchQuery.NotSeen : SearchQuery.All;
                var uids = await inbox.SearchAsync(query, cancellationToken);
                var result = new List<EmailMessage>();

                foreach (var uid in uids)
                {
                    var message = await inbox.GetMessageAsync(uid, cancellationToken);
                    result.Add(ParseMimeMessage(message, options));
                }

                return result;
            }
            else // POP3
            {
                using var client = new Pop3Client();
                await client.ConnectAsync(options.Host, options.Port, options.UseSsl, cancellationToken);
                await client.AuthenticateAsync(options.Username, options.Password, cancellationToken);

                var count = client.Count;
                var result = new List<EmailMessage>();
                for (int i = 0; i < count; i++)
                {
                    var message = await client.GetMessageAsync(i, cancellationToken);
                    result.Add(ParseMimeMessage(message, options));
                }
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed retrieving emails via {Protocol}", options.Protocol);
            return new List<EmailMessage>();
        }
    }

    private static EmailMessage ParseMimeMessage(MimeMessage message, EmailRetrievalOptions options)
    {
        var html = message.HtmlBody ?? string.Empty;
        var status = DetectStatus(html, options);

        return new EmailMessage
        {
            MessageId = message.MessageId,
            From = message.From.ToString(),
            Subject = message.Subject,
            Body = message.TextBody ?? message.HtmlBody ?? string.Empty,
            ReceivedDate = message.Date.DateTime,
            Attachments = message.Attachments.Select(a => new EmailAttachment
            {
                AttachmentId = a.ContentId,
                FileName = a is MimePart mp ? mp.FileName : "unknown",
                Size = a is MimePart mp2 ? mp2.Size : 0,
                ContentType = a.ContentType.MimeType
            }).ToList()
        };
    }

    private static string DetectStatus(string html, EmailRetrievalOptions opts)
    {
        if (string.IsNullOrWhiteSpace(html)) return "Unknown";
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var text = doc.DocumentNode.InnerText.ToLowerInvariant();
        if (opts.SuccessKeywords.Any(k => text.Contains(k.ToLowerInvariant()))) return "Success";
        if (opts.FailureKeywords.Any(k => text.Contains(k.ToLowerInvariant()))) return "Failure";
        return "Unknown";
    }

    public async Task DisconnectAsync()
    {
        if (_imapClient != null && _imapClient.IsConnected)
        {
            await _imapClient.DisconnectAsync(true);
            _logger.LogInformation("Successfully disconnected IMAP client");
        }

        if (_smtpClient != null && _smtpClient.IsConnected)
        {
            await _smtpClient.DisconnectAsync(true);
            _logger.LogInformation("Successfully disconnected SMTP client");
        }
    }

    public void Dispose()
    {
        _imapClient?.Dispose();
        _popClient?.Dispose();
        _smtpClient?.Dispose();
    }
} 