using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntegrationPlatform.Contracts.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace IntegrationPlatform.Authentication.Providers;

/// <summary>
/// Retrieves access tokens from Azure AD using the OAuth2 client-credential flow.
/// </summary>
public sealed class AzureAdClientCredentialProvider : IAuthenticationProvider
{
    private readonly IConfidentialClientApplication _app;
    private readonly string[] _scopes;
    private readonly ILogger<AzureAdClientCredentialProvider> _logger;

    public AuthFlowType FlowType => AuthFlowType.OAuth2ClientCredentials;

    public AzureAdClientCredentialProvider(string tenantId,
                                           string clientId,
                                           string clientSecret,
                                           string[] scopes,
                                           ILogger<AzureAdClientCredentialProvider> logger)
    {
        _logger = logger;
        _scopes = scopes;
        _app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                    .WithClientSecret(clientSecret)
                                                    .WithTenantId(tenantId)
                                                    .Build();
    }

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _app.AcquireTokenForClient(_scopes).ExecuteAsync(cancellationToken);
            return new AuthResult
            {
                AccessToken = result.AccessToken,
                ExpiresOn = result.ExpiresOn.DateTime
            };
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "Azure AD token acquisition failed: {Error}", ex.ErrorCode);
            throw;
        }
    }
}
