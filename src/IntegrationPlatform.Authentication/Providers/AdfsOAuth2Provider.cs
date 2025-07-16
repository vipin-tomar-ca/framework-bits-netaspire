using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IntegrationPlatform.Contracts.Authentication;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;

namespace IntegrationPlatform.Authentication.Providers;

/// <summary>
/// Acquires tokens from an on-prem ADFS server using the OAuth2 client-credential or ROPC flow.
/// </summary>
public sealed class AdfsOAuth2Provider : IAuthenticationProvider
{
    private readonly HttpClient _http;
    private readonly Uri _tokenEndpoint;
    private readonly ILogger<AdfsOAuth2Provider> _logger;

    public AuthFlowType FlowType => AuthFlowType.OAuth2ClientCredentials;

    public AdfsOAuth2Provider(string adfsBaseUrl, ILogger<AdfsOAuth2Provider> logger, HttpMessageHandler? handler = null)
    {
        _logger = logger;
        _http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _tokenEndpoint = new Uri(new Uri(adfsBaseUrl), "/adfs/oauth2/token");
    }

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, string>
        {
            ["client_id"] = request.ClientId,
            ["client_secret"] = request.ClientSecret,
            ["grant_type"] = string.IsNullOrEmpty(request.Username) ? "client_credentials" : "password",
            ["resource"] = request.Scopes.Length > 0 ? request.Scopes[0] : "",
        };
        if (!string.IsNullOrEmpty(request.Username))
        {
            body["username"] = request.Username;
            body["password"] = request.Password;
        }

        using var content = new FormUrlEncodedContent(body);
        var response = await _http.PostAsync(_tokenEndpoint, content, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("ADFS token retrieval failed: {Status} {Body}", response.StatusCode, json);
            throw new InvalidOperationException($"ADFS token retrieval failed: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(json);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString() ?? string.Empty;
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        return new AuthResult
        {
            AccessToken = accessToken,
            ExpiresOn = DateTime.UtcNow.AddSeconds(expiresIn)
        };
    }
}
