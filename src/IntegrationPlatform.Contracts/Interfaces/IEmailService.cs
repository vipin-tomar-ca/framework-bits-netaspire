namespace IntegrationPlatform.Contracts.Interfaces;

public interface IEmailService
{
    Task<bool> ConnectAsync(string host, int port, string username, string password);
    Task<bool> SendEmailAsync(string to, string subject, string body, List<string>? attachments = null);
    Task<List<EmailMessage>> GetUnreadEmailsAsync();
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

public class EmailAttachment
{
    public string AttachmentId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
} 