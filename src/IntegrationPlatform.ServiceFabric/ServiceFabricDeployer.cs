using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IntegrationPlatform.Contracts.Deployment;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Client;
using Microsoft.ServiceFabric.Common;

namespace IntegrationPlatform.ServiceFabric;

public sealed class ServiceFabricDeployer : IServiceFabricDeployer
{
    private readonly ILogger<ServiceFabricDeployer> _logger;

    public ServiceFabricDeployer(ILogger<ServiceFabricDeployer> logger)
    {
        _logger = logger;
    }

    public async Task<bool> DeployAsync(ServiceFabricDeployOptions options, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(options.ApplicationPackagePath))
        {
            _logger.LogError("Application package path does not exist: {Path}", options.ApplicationPackagePath);
            return false;
        }

        var client = new ServiceFabricClientBuilder()
                            .UseEndpoints(new Uri(options.ClusterEndpoint))
                            .Build();

        try
        {
            _logger.LogInformation("Copying package to image store...");
            var storePath = $"{options.ApplicationTypeName}_{options.ApplicationTypeVersion}";
            await client.ImageStore.CopyToImageStoreAsync(storeRelativePath: storePath,
                                                         sourcePath: options.ApplicationPackagePath,
                                                         timeout: 600,
                                                         cancellationToken: cancellationToken);

            _logger.LogInformation("Registering application type...");
            await client.ApplicationTypes.RegisterApplicationTypeAsync(storePath,
                                                                        cancellationToken: cancellationToken);

            _logger.LogInformation("Creating/Upgrading application...");
            var appName = new ApplicationName($"fabric:/{options.ApplicationTypeName}");
            ApplicationResourceDescription appDesc = new(options.ApplicationTypeName, options.ApplicationTypeVersion, appName.ToString());
            await client.Applications.CreateOrUpdateAsync(appName, appDesc, cancellationToken);

            _logger.LogInformation("Service Fabric deployment successful.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service Fabric deployment failed");
            return false;
        }
    }
}
