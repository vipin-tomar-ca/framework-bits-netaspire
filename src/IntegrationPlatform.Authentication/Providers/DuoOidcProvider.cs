using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntegrationPlatform.Contracts.Authentication;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Authentication.Providers;

/// <summary>
/// Retrieves tokens from Duo OIDC (Client Credentials Grant) as a 2FA enforcement mechanism.
/// </summary>
public sealed class DuoOidcProvider : IAuthenticationProvider
{
    private readonly HttpClient _http;
    private readonly Uri _tokenEndpoint;
    private readonly ILogger<DuoOidcProvider> _logger;

    public AuthFlowType FlowType => AuthFlowType.OAuth2ClientCredentials;

    public DuoOidcProvider(string duoApiHost, ILogger<DuoOidcProvider> logger, HttpMessageHandler? handler = null)
    {
        _logger = logger;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _tokenEndpoint = new Uri($"https://{duoApiHost}/oauth/v1/token");
    }

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = request.ClientId,
            ["client_secret"] = request.ClientSecret,
            ["scope"] = string.Join(' ', request.Scopes)
        };
        var content = new FormUrlEncodedContent(body);
        var response = await _http.PostAsync(_tokenEndpoint, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Duo OIDC token error: {Status} {Body}", response.StatusCode, json);
            throw new InvalidOperationException("Duo OIDC token error");
        }
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        var expires = doc.RootElement.GetProperty("expires_in").GetInt32();
        return new AuthResult { AccessToken = token, ExpiresOn = DateTime.UtcNow.AddSeconds(expires) };
    }
}
