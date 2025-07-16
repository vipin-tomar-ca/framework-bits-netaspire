# IntegrationPlatform.ServiceFabric

Utilities for packaging and deploying Service Fabric applications from code or CI pipelines.

## Projects

| Project | Purpose |
|---------|---------|
| `IntegrationPlatform.ServiceFabric` | Contains `ServiceFabricPackager` (builds application package folders/ZIPs) and `ServiceFabricDeployer` (copies, registers and upgrades the app in a cluster). |

## Quick start

1. **Package** your solution:

```csharp
var packager = new ServiceFabricPackager(logger);
var pkgPath = await packager.BuildAsync(new ServiceFabricPackageOptions
{
    SourceRoot = "../src",              // where .csproj live
    ManifestSource = "./Manifests",     // has ApplicationManifest.xml etc.
    OutputPath = "./pkg/MyApp",
    AutoIncrementVersion = true,         // bumps manifest versions automatically
    ZipOutput = true                     // produces ./pkg/MyApp.zip
});
```

2. **Deploy** to a cluster:

```csharp
services.AddServiceFabricDeployer();
...
var success = await _deployer.DeployAsync(new ServiceFabricDeployOptions
{
    ApplicationPackagePath = pkgPath,            // folder or .zip
    ApplicationTypeName = "MyApp",
    ApplicationTypeVersion = "1.0.1",          // must match manifest (auto-incremented)
    ClusterEndpoint = "http://localhost:19080" // or your cluster url
});
```

## CI pipeline example

```bash
# Publish package
 dotnet run --project build/PackagerCli.csproj -- --src ../src --manifest ./Manifests --out ./pkg --zip

# Deploy (requires sfctl or cluster connection)
 dotnet run --project build/DeployerCli.csproj -- --pkg ./pkg/MyApp.zip --cluster http://cl1.mycorp.com:19080
```

## Features

* Publishes all `*Service.csproj` via `dotnet publish -c Release`.
* Copies outputs into `Code` folders automatically.
* Copies existing manifests unchanged and can bump their version numbers (`AutoIncrementVersion`).
* Optionally zips the final package for faster transfer.
* `ServiceFabricDeployer` uploads the package to Image Store, registers type, upgrades/creates the app.
* Full DI support (`AddServiceFabricDeployer`).

## Requirements

* .NET 6 SDK on build agent.
* Service Fabric SDK/cluster for deployment (`Microsoft.ServiceFabric.Client` uses MS Fabric HTTP APIs).

---
Built with ❤️ by the Integration Platform team.
