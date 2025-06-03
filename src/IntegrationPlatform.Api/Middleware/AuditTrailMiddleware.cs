using System.Text;
using IntegrationPlatform.Contracts.Attributes;
using IntegrationPlatform.Contracts.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace IntegrationPlatform.Api.Middleware;

public class AuditTrailMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditTrailMiddleware> _logger;
    private readonly AuditTrailOptions _options;

    public AuditTrailMiddleware(
        RequestDelegate next,
        ILogger<AuditTrailMiddleware> logger,
        IOptions<AuditTrailOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var attribute = endpoint?.Metadata.GetMetadata<AuditTrailAttribute>();

        if (attribute == null)
        {
            await _next(context);
            return;
        }

        var options = attribute.ToOptions();
        var entry = new AuditTrailEntry
        {
            Operation = attribute.Operation,
            OperationType = attribute.OperationType,
            RequestPath = context.Request.Path,
            RequestMethod = context.Request.Method,
            IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            UserId = context.User?.FindFirst("sub")?.Value ?? "Anonymous",
            UserName = context.User?.FindFirst("name")?.Value ?? "Anonymous"
        };

        var startTime = DateTime.UtcNow;

        try
        {
            if (options.LogRequestBody && context.Request.Body.CanRead)
            {
                context.Request.EnableBuffering();
                var body = await ReadRequestBodyAsync(context.Request, options);
                entry.RequestBody = body;
            }

            if (options.LogHeaders)
            {
                var headers = context.Request.Headers
                    .Where(h => !options.SensitiveHeaders.Contains(h.Key))
                    .ToDictionary(h => h.Key, h => h.Value.ToString());
                entry.AdditionalData["RequestHeaders"] = System.Text.Json.JsonSerializer.Serialize(headers);
            }

            if (options.LogQueryParameters)
            {
                var queryParams = context.Request.Query
                    .Where(q => !options.SensitiveParameters.Contains(q.Key))
                    .ToDictionary(q => q.Key, q => q.Value.ToString());
                entry.AdditionalData["QueryParameters"] = System.Text.Json.JsonSerializer.Serialize(queryParams);
            }

            // Store the original response body stream
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            // Read the response body
            if (options.LogResponseBody)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var response = await new StreamReader(responseBody).ReadToEndAsync();
                entry.AdditionalData["ResponseBody"] = response.Length > options.MaxBodyLength
                    ? response[..options.MaxBodyLength] + "..."
                    : response;
            }

            // Copy the response body back to the original stream
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);

            entry.ResponseStatus = context.Response.StatusCode.ToString();
        }
        catch (Exception ex)
        {
            entry.ResponseStatus = "500";
            entry.ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            entry.Duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Audit Trail: {Operation} ({OperationType}) - {Status} - {Duration}ms",
                entry.Operation,
                entry.OperationType,
                entry.ResponseStatus,
                entry.Duration.TotalMilliseconds);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request, AuditTrailOptions options)
    {
        request.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(request.Body).ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);

        return body.Length > options.MaxBodyLength
            ? body[..options.MaxBodyLength] + "..."
            : body;
    }
} 