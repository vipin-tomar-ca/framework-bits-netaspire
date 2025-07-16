using System.Threading;
using System.Threading.Tasks;
using IntegrationPlatform.Contracts.Authentication;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Authentication.Providers;

/// <summary>
/// Interactive OAuth2 authorization-code / MFA provider (opens system browser or device code).
/// </summary>
public sealed class InteractiveMfaProvider : IAuthenticationProvider
{
    private readonly IPublicClientApplication _app;
    private readonly ILogger<InteractiveMfaProvider> _logger;
    private readonly string[] _scopes;

    public AuthFlowType FlowType => AuthFlowType.OAuth2AuthorizationCode;

    public InteractiveMfaProvider(string clientId, string tenantId, string[] scopes, ILogger<InteractiveMfaProvider> logger)
    {
        _logger = logger;
        _scopes = scopes;
        _app = PublicClientApplicationBuilder.Create(clientId)
                                            .WithTenantId(tenantId)
                                            .WithRedirectUri("http://localhost")
                                            .Build();
    }

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _app.AcquireTokenInteractive(_scopes)
                                   .WithPrompt(Prompt.SelectAccount)
                                   .ExecuteAsync(cancellationToken);
            return new AuthResult
            {
                AccessToken = result.AccessToken,
                ExpiresOn = result.ExpiresOn.DateTime,
                RefreshToken = result is IAuthenticationResult authRes && authRes.AuthenticationResultMetadata.TokenSource == TokenSource.IdentityProvider
                                ? authRes.RefreshToken
                                : string.Empty
            };
        }
        catch (MsalException ex)
        {
            _logger.LogError(ex, "Interactive MFA failed: {Error}", ex.ErrorCode);
            throw;
        }
    }
}
