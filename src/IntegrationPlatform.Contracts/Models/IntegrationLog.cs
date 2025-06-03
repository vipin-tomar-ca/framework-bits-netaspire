namespace IntegrationPlatform.Contracts.Models;

public class IntegrationLog
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public IntegrationJob? Job { get; set; }
} 