using Microsoft.Extensions.Logging;
using Musaed.Core.Dtos;
using Musaed.Core.Interfaces;
using Musaed.Wpf.ViewModels;
using System;
using System.IO;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Musaed.Wpf;

/// <summary>
/// Orchestrates the entire application startup sequence and returns the final settings.
/// </summary>
public class Bootstrapper
{
    private const string AppSettingsCacheKey = "last_known_good_appsettings";

    private readonly ILogger<Bootstrapper> _logger;
    private readonly IConfigService _config;
    private readonly IAuthService _auth;
    private readonly IBackendApiClient _api;
    private readonly ICacheService _cache;
    private readonly IPackageManager _packageManager;
    private readonly IProcessManager _processManager;
    private readonly LoadingViewModel _vm;

    public Bootstrapper(
        ILogger<Bootstrapper> logger, IConfigService config, IAuthService auth,
        IBackendApiClient api, ICacheService cache, IPackageManager packageManager,
        IProcessManager processManager, LoadingViewModel vm)
    {
        _logger = logger; _config = config; _auth = auth; _api = api;
        _cache = cache; _packageManager = packageManager; _processManager = processManager;
        _vm = vm;
    }

    /// <summary>
    /// Executes the entire multi-step startup flow and returns the result.
    /// </summary>
    /// <returns>The AppSettingsDto on success, or null on a critical failure.</returns>
    public async Task<AppSettingsDto> RunAsync()
    {
        _logger.LogInformation("Bootstrapper sequence started.");
        AppSettingsDto appSettings = null;
        Guid appId = Guid.Empty;

        try
        {
            _vm.StatusMessage = "Authenticating...";
            await _auth.GetAccessTokenAsync();

            _vm.StatusMessage = "Locating application on server...";
            var appName = _config.Config.AppName;
            appId = await _api.GetAppIdAsync(appName);
            _logger.LogInformation("Discovered App ID: {AppId}", appId);

            _vm.StatusMessage = "Fetching remote configuration...";
            appSettings = await _api.GetRemoteSettingsAsync(appId, _config.Config.ConfigName, _config.Config.AssetName);

            _logger.LogInformation("Successfully fetched settings from server. Caching for offline use...");
            _cache.Save(appSettings, AppSettingsCacheKey);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogCritical(ex, "Authentication failed. Halting startup.");
            _vm.StatusMessage = "Authentication Error. Please check credentials and network.";
            await Task.Delay(3000); // Give user time to read
            return null; // Signal failure
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch settings from server. Attempting to use local cache.");
            _vm.StatusMessage = "Connection failed. Attempting to start in offline mode...";
            await Task.Delay(2000);

            appSettings = _cache.Load<AppSettingsDto>(AppSettingsCacheKey);
        }

        if (appSettings == null)
        {
            _logger.LogCritical("Failed to start: No server settings and no cached settings available.");
            _vm.StatusMessage = "Offline mode failed. No cached data found.";
            await Task.Delay(3000); // Give user time to read
            return null; // Signal failure
        }

        try
        {
            _logger.LogInformation("Using settings for package version {Version}", appSettings.ServerSettings.Version);

            _vm.StatusMessage = "Preparing application package...";
           // await _packageManager.EnsurePackageIsReadyAsync(appId, appSettings.ServerSettings);

            _vm.StatusMessage = "Starting local server...";
           // await _processManager.StartServerAsync(appSettings);

            _logger.LogInformation("Bootstrapper sequence completed successfully.");
            return appSettings; // Signal success
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "A critical error occurred during package or process startup.");
            _vm.StatusMessage = $"Error: {ex.Message}";
            await Task.Delay(5000); // Give user time to read the specific error
            return null; // Signal failure
        }
    }
}