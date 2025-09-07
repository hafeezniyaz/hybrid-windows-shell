using Microsoft.Extensions.Logging;
using Musaed.Core.Dtos;
using Musaed.Core.Interfaces;
using Musaed.Infrastructure.Helpers;
using System.Diagnostics;

public class ProcessManagerService : IProcessManager
{
    private const int HealthCheckMaxRetries = 30;
    private const int HealthCheckIntervals = 2000;

    private readonly ILogger<ProcessManagerService> _logger;
    private readonly IPackageManager _packageManager;
    private Process _serverProcess;
    private Mutex _serverMutex;
    private string _mutexName;
    private AppSettingsDto _appSettings;

    public ProcessManagerService(
        ILogger<ProcessManagerService> logger,
        IPackageManager packageManager
        )
    {
        _logger = logger;
        _packageManager = packageManager;
    }
    public async Task StartServerAsync(AppSettingsDto appSettings, Guid appId)
    {
        var processName = Path.GetFileNameWithoutExtension(_packageManager.ServerExePath);
        _mutexName = $"Global\\{appId}-{appSettings.ServerSettings.Version}-ServerMutex";
        _appSettings = appSettings;
        //_serverMutex = new Mutex(true, _mutexName, out bool isCreatedNew);

        var existingServerProcess = Process.GetProcessesByName(processName).FirstOrDefault();

        if (existingServerProcess != null)
        {
            _serverProcess = existingServerProcess;
            await AttachToExistingServer();
        }
        else
        {
            await HandleNewServerStartup();
        }

    }

    private async Task AttachToExistingServer()
    {
        _logger.LogInformation($"Server is already running with processId {_serverProcess.Id}..." +
            $"Checking health to attach..");
        try
        {
            await WaitforServerTaskToComplete();
            _serverMutex = new Mutex(true, _mutexName, out _);
            _logger.LogInformation($"attached to Server already running with processId {_serverProcess.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "existing server did not respond in timely manner. " +
                "killing the server assuming its unhealthy...");

            ProcessShutdownHelper.GracefullyShutdown(_serverProcess, _logger);

            await HandleNewServerStartup();
        }
    }

    private async Task HandleNewServerStartup()
    {
        _serverMutex = new Mutex(true, _mutexName, out bool isCreatedNew);
        if (isCreatedNew)
        {
            await RunServer();
        }
        else
        {
            await WaitforServerTaskToComplete();
        }
    }

    private async Task RunServer()
    {
        var exePath = _packageManager.ServerExePath;
        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
        {
            throw new FileNotFoundException("Server executable was not found in the expected path", exePath);
        }

        _logger.LogInformation($"starting server process {exePath}");

        var startInfo = new ProcessStartInfo(exePath)
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(exePath)
        };

        _serverProcess = Process.Start(startInfo);
        if (_serverProcess == null)
        {
            _serverMutex?.ReleaseMutex();
            throw new InvalidOperationException($"Failed to start the server process from the{exePath}");
        }

        _logger.LogInformation($"server process started with process id {_serverProcess.Id}");
        await WaitforServerTaskToComplete();

    }

    private async Task WaitforServerTaskToComplete()
    {


        var healthCheckUrl = new Uri(new Uri(_appSettings.ServerSettings.RootUrl), _appSettings.ServerSettings.HealthApiPath);
        var httpClientHandler = new HttpClientHandler();


        httpClientHandler.UseDefaultCredentials = _appSettings.ServerSettings.UseWindowsAuth == true;

        using var httpClient = new HttpClient(httpClientHandler);

        if (_serverProcess == null || _serverProcess.HasExited)
        {

            throw new InvalidOperationException("Server has already existed.");
        };

        for (int i = 0; i < HealthCheckMaxRetries; i++)
        {

            try
            {
                var response = await httpClient.GetAsync(healthCheckUrl);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Health check with process" +
                        $" {_serverProcess.ProcessName} successfull");
                    return;
                }


            }
            catch (HttpRequestException)
            {
                _logger.LogInformation("Waiting for successfull health check...");
            }

            await Task.Delay(HealthCheckIntervals);
        }

        _logger.LogCritical($"Server process {_serverProcess.ProcessName} " +
            $"did not become healthy after {HealthCheckMaxRetries} attemps");

        await StopServer();

        throw new TimeoutException($"Process {_serverProcess.ProcessName}" +
            $" failed to respond in timely manner");
    }

    public async Task StopServer(bool? closeAllInstancedAndServer = false)
    {

        var exePath = _packageManager.ServerExePath;
        var processName = Path.GetFileNameWithoutExtension(exePath);
        var serverProcesses = Process.GetProcessesByName(processName);

        if (closeAllInstancedAndServer == true)
        {
            //close all app instances excecpt this one
            await ProcessShutdownHelper.CloseAllOtherInstances(processName, _logger);
            return;
        }

        if (_appSettings != null && _appSettings.ServerSettings.ContinueInBackgroundEnabled == true) return;

        var currentAppProcess = Process.GetCurrentProcess();
        var clientProcesses = Process.GetProcessesByName(currentAppProcess.ProcessName);

        if (clientProcesses.Length > 1) return;

        if (serverProcesses.Length == 0) return;

        await ProcessShutdownHelper.CreateAndRunShutDownTask(serverProcesses, _logger);

    }


    public void Dispose()
    {
        _serverProcess?.Dispose();
    }
}