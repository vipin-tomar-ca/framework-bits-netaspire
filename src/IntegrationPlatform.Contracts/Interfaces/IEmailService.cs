namespace IntegrationPlatform.Contracts.Interfaces;

public interface IEmailService
{
    Task<bool> ConnectAsync(string host, int port, string username, string password);
    Task<bool> SendEmailAsync(string to, string subject, string body, List<string>? attachments = null);
    Task<List<EmailMessage>> GetUnreadEmailsAsync();
    Task<List<EmailMessage>> RetrieveEmailsAsync(EmailRetrievalOptions options, CancellationToken cancellationToken = default);
    Task<bool> MarkEmailAsReadAsync(string messageId);
    Task<bool> DownloadAttachmentAsync(string messageId, string attachmentId, string localPath);
    Task DisconnectAsync();
}

public class EmailMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = new();
}

public class EmailRetrievalOptions
{
    // Connection
    public EmailProtocol Protocol { get; set; } = EmailProtocol.Imap;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 0;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Folder { get; set; } = "INBOX";
    public bool UnreadOnly { get; set; } = true;

    // Parsing
    public List<string> SuccessKeywords { get; set; } = new() { "success" };
    public List<string> FailureKeywords { get; set; } = new() { "failure", "error" };
}
    public EmailProtocol Protocol { get; set; } = EmailProtocol.Imap;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 0;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Folder { get; set; } = "INBOX";
    public bool UnreadOnly { get; set; } = true;
}

public enum EmailProtocol
{
    Imap,
    Pop3
}

public class EmailAttachment
{
    public string AttachmentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
} 