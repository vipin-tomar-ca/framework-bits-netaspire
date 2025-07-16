namespace IntegrationPlatform.Contracts.Authentication;

/// <summary>
/// Specifies the high-level authentication flow being executed.
/// </summary>
public enum AuthFlowType
{
    /// <summary>OAuth2 confidential-client flow (client credentials).</summary>
    OAuth2ClientCredentials,
    /// <summary>OAuth2 auth-code / interactive user login.</summary>
    OAuth2AuthorizationCode,
    /// <summary>Single Sign-On provided by the host (e.g., Windows Integrated).</summary>
    SsoIntegrated
}

/// <summary>
/// Input parameters for an authentication request.  Concrete providers may use a subset.
/// </summary>
public record AuthRequest
{
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty; // or certificate thumbprint etc.
    public string[] Scopes { get; init; } = Array.Empty<string>();
    public string RedirectUri { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Result of an authentication exchange.
/// </summary>
public record AuthResult
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresOn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}
