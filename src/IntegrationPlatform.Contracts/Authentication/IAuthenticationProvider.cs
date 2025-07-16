using System.Threading;
using System.Threading.Tasks;

namespace IntegrationPlatform.Contracts.Authentication;

/// <summary>
/// Generic abstraction for acquiring access tokens or authentication material for downstream services.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>Describes the underlying flow.</summary>
    AuthFlowType FlowType { get; }

    /// <summary>
    /// Performs the authentication exchange and returns an <see cref="AuthResult"/>.
    /// </summary>
    Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken = default);
}
