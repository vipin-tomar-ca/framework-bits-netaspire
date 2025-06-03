namespace IntegrationPlatform.Contracts.Models;

public class IntegrationJob
{
    public int Id { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<FileTransfer> FileTransfers { get; set; } = new();
    public List<IntegrationLog> Logs { get; set; } = new();
} 