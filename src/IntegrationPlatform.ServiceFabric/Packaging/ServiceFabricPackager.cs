using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IntegrationPlatform.ServiceFabric.Packaging;

/// <summary>
/// Very light-weight helper that builds a Service Fabric application package folder by
/// 1. Publishing each service project (dotnet publish ‑c Release)
/// 2. Copying outputs to Code folders
/// 3. Copying manifests untouched (advanced users may transform them beforehand)
/// 4. Optionally zips the result
///
/// NOTE: For complex scenarios you may still prefer the official MSBuild SDK or Azure DevOps tasks.
/// </summary>
public sealed class ServiceFabricPackager
{
    private readonly ILogger<ServiceFabricPackager> _logger;

    public ServiceFabricPackager(ILogger<ServiceFabricPackager> logger)
    {
        _logger = logger;
    }

    public async Task<string> BuildAsync(ServiceFabricPackageOptions opts, CancellationToken ct = default)
    {
        if (!Directory.Exists(opts.SourceRoot)) throw new DirectoryNotFoundException(opts.SourceRoot);
        Directory.CreateDirectory(opts.OutputPath);

        // 1. find *.csproj that contain ServiceFabricService tags (heuristic: *Service)
        var serviceProjects = Directory.GetFiles(opts.SourceRoot, "*Service.csproj", SearchOption.AllDirectories);
        foreach (var proj in serviceProjects)
        {
            _logger.LogInformation("Publishing {Proj}...", Path.GetFileName(proj));
            await RunProcessAsync("dotnet", $"publish \"{proj}\" -c Release -o \"{opts.TempPublish}\"", ct);

            var serviceName = Path.GetFileNameWithoutExtension(proj);
            var destCode = Path.Combine(opts.OutputPath, serviceName, "Code");
            Directory.CreateDirectory(destCode);
            foreach (var file in Directory.EnumerateFiles(opts.TempPublish))
                File.Copy(file, Path.Combine(destCode, Path.GetFileName(file)), overwrite: true);
            Directory.Delete(opts.TempPublish, recursive: true);
        }

        // 2. copy manifest folder as-is
        if (!string.IsNullOrEmpty(opts.ManifestSource) && Directory.Exists(opts.ManifestSource))
        {
            foreach (var file in Directory.EnumerateFiles(opts.ManifestSource, "*.*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(opts.ManifestSource, file);
                var dest = Path.Combine(opts.OutputPath, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                File.Copy(file, dest, true);
            }
        }

        // 3. Auto increment versions if requested
        if (opts.AutoIncrementVersion)
        {
            var appManifest = Directory.GetFiles(opts.OutputPath, "ApplicationManifest.xml", SearchOption.AllDirectories).FirstOrDefault();
            if (appManifest != null)
            {
                BumpVersion(appManifest);
                // bump service manifests as well
                foreach (var svcManifest in Directory.GetFiles(Path.GetDirectoryName(appManifest)!, "ServiceManifest.xml", SearchOption.AllDirectories))
                    BumpVersion(svcManifest);
            }
        }

        // 4. Optionally zip
        if (opts.ZipOutput)
        {
            var zipPath = opts.OutputPath.TrimEnd(Path.DirectorySeparatorChar) + ".zip";
            if (File.Exists(zipPath)) File.Delete(zipPath);
            System.IO.Compression.ZipFile.CreateFromDirectory(opts.OutputPath, zipPath);
            _logger.LogInformation("Service Fabric package built at {Zip}", zipPath);
            return zipPath;
        }

        _logger.LogInformation("Service Fabric package built at {Dir}", opts.OutputPath);
        return opts.OutputPath;
    }

    private static async Task RunProcessAsync(string fileName, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo(fileName, args) { RedirectStandardOutput = true, RedirectStandardError = true };
        var p = Process.Start(psi)!;
        await p.WaitForExitAsync(ct);
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"Command {fileName} {args} failed:\n{await p.StandardError.ReadToEndAsync(ct)}");
    }
}

public record ServiceFabricPackageOptions
{
    public string SourceRoot { get; init; } = string.Empty; // solution src folder
    public string OutputPath { get; init; } = "./pkg";
    public string ManifestSource { get; init; } = "./SFManifests";
    public bool ZipOutput { get; init; } = false;
    public bool AutoIncrementVersion { get; init; } = false;
    internal string TempPublish => Path.Combine(Path.GetTempPath(), "sfpub_" + Guid.NewGuid().ToString("N"));
}
