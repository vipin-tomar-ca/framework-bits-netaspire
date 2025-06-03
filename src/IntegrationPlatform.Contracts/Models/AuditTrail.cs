namespace IntegrationPlatform.Contracts.Models;

public class AuditTrailEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Operation { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public string ResponseStatus { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

public class AuditTrailOptions
{
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;
    public bool LogHeaders { get; set; } = true;
    public bool LogQueryParameters { get; set; } = true;
    public int MaxBodyLength { get; set; } = 1000;
    public string[] SensitiveHeaders { get; set; } = new[] { "Authorization", "Cookie" };
    public string[] SensitiveParameters { get; set; } = new[] { "password", "token", "secret" };
} 