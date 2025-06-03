using IntegrationPlatform.Contracts.Models;

namespace IntegrationPlatform.Contracts.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuditTrailAttribute : Attribute
{
    public string Operation { get; }
    public string OperationType { get; }
    public bool LogRequestBody { get; set; } = true;
    public bool LogResponseBody { get; set; } = true;
    public bool LogHeaders { get; set; } = true;
    public bool LogQueryParameters { get; set; } = true;
    public int MaxBodyLength { get; set; } = 1000;
    public string[] SensitiveHeaders { get; set; } = Array.Empty<string>();
    public string[] SensitiveParameters { get; set; } = Array.Empty<string>();

    public AuditTrailAttribute(string operation, string operationType)
    {
        Operation = operation;
        OperationType = operationType;
    }

    public AuditTrailOptions ToOptions()
    {
        return new AuditTrailOptions
        {
            LogRequestBody = LogRequestBody,
            LogResponseBody = LogResponseBody,
            LogHeaders = LogHeaders,
            LogQueryParameters = LogQueryParameters,
            MaxBodyLength = MaxBodyLength,
            SensitiveHeaders = SensitiveHeaders,
            SensitiveParameters = SensitiveParameters
        };
    }
} 