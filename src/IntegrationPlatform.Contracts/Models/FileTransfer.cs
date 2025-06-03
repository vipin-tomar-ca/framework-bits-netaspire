namespace IntegrationPlatform.Contracts.Models;

public class FileTransfer
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime TransferDate { get; set; }
    public IntegrationJob? Job { get; set; }
} 