using System.Threading;
using System.Threading.Tasks;

namespace IntegrationPlatform.Contracts.Deployment;

public interface IServiceFabricDeployer
{
    Task<bool> DeployAsync(ServiceFabricDeployOptions options, CancellationToken cancellationToken = default);
}

public record ServiceFabricDeployOptions
{
    public string ApplicationPackagePath { get; init; } = string.Empty;
    public string ApplicationTypeName { get; init; } = string.Empty;
    public string ApplicationTypeVersion { get; init; } = string.Empty;
    public string ClusterEndpoint { get; init; } = string.Empty; // e.g. http://localhost:19080
}
