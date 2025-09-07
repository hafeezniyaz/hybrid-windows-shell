using Microsoft.Extensions.Logging;
using Musaed.Core.Dtos;
using Musaed.Core.Interfaces;
using Musaed.Infrastructure.Helpers;
using System;
using System.IO;
using System.IO.Compression; // Required for ZipFile
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Musaed.Infrastructure.Services;

public class PackageManagerService : IPackageManager
{
    public string ServerExePath { get; private set; }


    private readonly ILogger<PackageManagerService> _logger;
    private readonly IConfigService _config;
    private readonly IBackendApiClient _api;
    private readonly string _packageDowloadPath;
    public PackageManagerService(
        ILogger<PackageManagerService> logger,
        IConfigService config,
        IBackendApiClient api

        )
    {
        _logger = logger;
        _config = config;
        _api = api;
        _packageDowloadPath = Environment.ExpandEnvironmentVariables(
            _config.Config.PackageDownloadRootFolder);


    }
    public async Task EnsurePackageIsReadyAsync(Guid appId, ServerSettingsDto serverSettings)
    {
        var versionsFolderPath = Path.Combine(
            _packageDowloadPath,
            "packages",
            serverSettings.AppName,
            "versions"

            );

        var packagePath = Path.Combine(versionsFolderPath, serverSettings.Version);

        ServerExePath = Path.Combine(packagePath, serverSettings.ExeName);

        if (Directory.Exists(packagePath))
        {
            _logger.LogInformation($"Package with version {serverSettings.Version} " +
                $"is available locally for the app {serverSettings.AppName}");
            return;

        }

        _logger.LogInformation($"Getting package details for app " +
            $"{serverSettings.AppName} with version {serverSettings.Version}");


        var packageMetadata = await _api.GetPackageMetadataAsync(appId, serverSettings.Version);

        _logger.LogInformation("Downloading the package...");

        using var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var packageData = await _api.DownloadPackageAsync(appId, serverSettings.Version, cancellationToken.Token);

        _logger.LogInformation("Validating the package...");
        if (!isChecksumValid(packageData, packageMetadata.Md5Checksum))
        {
            throw new InvalidDataException("Pakage validation failed. " +
                "the package might be corrupted or tampered");
        }

        _logger.LogInformation("Extracting package to ", packagePath);

        await UnpackPackageAtomically(packageData, packagePath, versionsFolderPath);


        _logger.LogInformation("Package extraction complete and ready to use");


    }

    private async Task UnpackPackageAtomically(byte[] packageData, string NewVersionPath, string versionFolderPath)
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        var tempUnpackPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var backupPath = $"{versionFolderPath}_backup";


        try
        {

            var processName = Path.GetFileNameWithoutExtension(ServerExePath);

            //close all instances
            await ProcessShutdownHelper.CloseAllOtherInstances(processName, _logger);

            File.WriteAllBytes(tempZipPath, packageData);

            Directory.CreateDirectory(tempUnpackPath);
            ZipFile.ExtractToDirectory(tempZipPath, tempUnpackPath);
            _logger.LogInformation($"package tempororily extracted to {tempUnpackPath}");

            if (Directory.Exists(versionFolderPath))
            {
                _logger.LogInformation($"creating backup at {backupPath}");
                Directory.Move(versionFolderPath, backupPath);
            }

            Directory.CreateDirectory(versionFolderPath);


            _logger.LogInformation($"Installing new version at {NewVersionPath}");

            Directory.Move(tempUnpackPath, NewVersionPath);

            if (Directory.Exists(backupPath))
            {
                _logger.LogInformation("deleting backup..");
                Directory.Delete(backupPath, recursive: true);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update, rolling back...");

            if (Directory.Exists(NewVersionPath))
            {
                Directory.Delete(NewVersionPath, recursive: true);
            }
            if (Directory.Exists(backupPath))
            {
                Directory.Move(backupPath, versionFolderPath);
            }

            throw new IOException("Failed to unpack the package .", ex);
        }
        finally
        {
            if (File.Exists(tempZipPath)) File.Delete(tempZipPath);
            if (Directory.Exists(tempUnpackPath)) Directory.Delete(tempUnpackPath, recursive: true);
        }

    }


    private bool isChecksumValid(byte[] data, string expectedChecksum)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(data);
        var actualChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        return actualChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase);
    }
}