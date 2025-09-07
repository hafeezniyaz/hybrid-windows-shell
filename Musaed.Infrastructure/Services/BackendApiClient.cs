using Microsoft.Extensions.Logging;
using Musaed.Core.Dtos;
using Musaed.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Musaed.Infrastructure.Services;

public class BackendApiClient : IBackendApiClient
{

private readonly ILogger<BackendApiClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly IConfigService _configService;
    public BackendApiClient(
        HttpClient httpClient,
        IConfigService configService,
        ILogger<BackendApiClient> logger)
    {
        _logger = logger;
        _configService = configService;
        _httpClient = httpClient;
    }

 public async Task<byte[]> DownloadPackageAsync(Guid appId, string version, CancellationToken cancellationToken = default)
    {
        var endpoint = new Uri(new Uri(_configService.Config.BackendRootUrl),
            $"api/v1/Apps/{appId}/Packages/{version}/download");

        var response = await _httpClient.GetAsync(
            endpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);

        memoryStream.Position = 0;

        return memoryStream.ToArray();

    }


    public async Task<Guid> GetAppIdAsync(string appName)
    {
        var endpoint = new Uri(new Uri(_configService.Config.BackendRootUrl),
                        $"api/v1/Apps?name={appName}&isActive=true&skip=0&top=1");

        _logger.LogInformation($"fetching app details from {endpoint.AbsoluteUri}");

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var app = await response.Content.ReadFromJsonAsync<GetAppsResponseDto>();

        if (app == null || app.TotalCount == 0)
        {
            throw new InvalidDataException($"requested app {appName} was not found or inactive");
        }

        return app.Items.First().Id;

    }

    public async Task<CredentialDto> GetCredentialAsync(Guid appId, string credentialName)
    {
        var endpoint = new Uri(new Uri(_configService.Config.BackendRootUrl),
                      $"api/v1/Apps/{appId}/Credentials/{credentialName}");

        _logger.LogInformation($"fetching credential details from {endpoint.AbsoluteUri}");

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var credential = await response.Content.ReadFromJsonAsync<CredentialDto>();
        return credential;
    }

    public async Task<PackageDetailsDto> GetPackageMetadataAsync(Guid appId, string version)
    {
        var endpoint = new Uri(new Uri(_configService.Config.BackendRootUrl),
                       $"api/v1/apps/{appId}/packages/{version}");

        _logger.LogInformation($"fetching asset details from {endpoint.AbsoluteUri}");

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();

        var package = await response.Content.ReadFromJsonAsync<PackageDetailsDto>();

        return package;
    }

    public async Task<AppSettingsDto> GetRemoteSettingsAsync(Guid appId, string ConfigName, string assetName)
    {
        var endpoint = new Uri(new Uri(_configService.Config.BackendRootUrl),
                      $"api/v1/Apps/{appId}/Configs/{ConfigName}/assets/{assetName}");

        _logger.LogInformation($"fetching asset details from {endpoint.AbsoluteUri}");

        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();


        var asset = await response.Content.ReadFromJsonAsync<ServerSettingsDto>();

        return new AppSettingsDto
        {
            ServerSettings = asset
        };

    }

}