using IntegrationPlatform.Contracts.Authentication;
using IntegrationPlatform.Authentication.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.Authentication;

/// <summary>
/// DI helpers for authentication providers.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureAdClientCredentialProvider(this IServiceCollection services,
                                                                        string tenantId,
                                                                        string clientId,
                                                                        string clientSecret,
                                                                        string[] scopes)
    {
        services.AddSingleton<IAuthenticationProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AzureAdClientCredentialProvider>>();
            return new AzureAdClientCredentialProvider(tenantId, clientId, clientSecret, scopes, logger);
        });
        return services;
    }
}
