using IntegrationPlatform.Contracts.Deployment;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationPlatform.ServiceFabric;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceFabricDeployer(this IServiceCollection services)
    {
        services.AddSingleton<IServiceFabricDeployer, ServiceFabricDeployer>();
        return services;
    }
}
