using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Musaed.Core.Interfaces;
using Musaed.Infrastructure.Logging;
using Musaed.Infrastructure.Services;
using Musaed.Wpf.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;
using Polly;
using Microsoft.Extensions.Http;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Polly.Extensions.Http;
using Musaed.Infrastructure.Handlers;
using System.Net.Http;
using Serilog;

namespace Musaed.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, services) =>
    {
        // Register services with the DI container
        services.AddSingleton<IConfigService, ConfigService>();

        // --- Authentication ---
        services.AddTransient<AuthenticationHandler>();
        services.AddHttpClient<IAuthService, AuthService>((serviceProvider, client) =>
        {
            var configService = serviceProvider.GetRequiredService<IConfigService>();
            client.BaseAddress = new Uri(configService.Config.BackendRootUrl);
        });

        // --- Shared HttpClient Configuration Logic ---
        Func<IServiceProvider, HttpClientHandler> primaryHandlerFactory = sp =>
        {
            var configService = sp.GetRequiredService<IConfigService>();
            var handler = new HttpClientHandler();
            if ("windows".Equals(configService.Config.AuthMode, StringComparison.OrdinalIgnoreCase))
            {
                handler.UseDefaultCredentials = true;
            }
            return handler;
        };

        var retryPolicy = (IServiceProvider provider, HttpRequestMessage request) =>
        {
            var logger = provider.GetRequiredService<ILogger<App>>();
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        // Use the logger to record retry attempts instead of Debug.WriteLine
                        logger.LogWarning(
                            "Request to {RequestUri} failed with {StatusCode}. Waiting {TimeSpan} before next retry. Attempt {RetryAttempt} of 3.",
                            outcome.Result?.RequestMessage?.RequestUri,
                            outcome.Result?.StatusCode,
                            timespan,
                            retryAttempt);
                    });
        };

        Action<IServiceProvider, HttpClient> configureClient = (sp, client) =>
        {
            var configService = sp.GetRequiredService<IConfigService>();
            client.BaseAddress = new Uri(configService.Config.BackendRootUrl);
        };

        // --- Main API Client (Typed) ---
        services.AddHttpClient<IBackendApiClient, BackendApiClient>(configureClient)
            .ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory)
            .AddHttpMessageHandler<AuthenticationHandler>()
            .AddPolicyHandler(retryPolicy);

        // --- Logging API Client (Named) ---
        // Register a named client with the same configuration for our logging sink
        services.AddHttpClient("LoggingClient", configureClient)
            .ConfigurePrimaryHttpMessageHandler(primaryHandlerFactory)
            .AddHttpMessageHandler<AuthenticationHandler>()
            .AddPolicyHandler(retryPolicy);

        // --- Other Services ---
        services.AddSingleton<ICacheService, JsonCacheService>();
        services.AddSingleton<IPackageManager, PackageManagerService>();
        services.AddSingleton<IProcessManager, ProcessManagerService>();
        services.AddSingleton<LoadingViewModel>();
        services.AddTransient<LoadingWindow>();
        services.AddTransient<BrowserWindow>();
        services.AddSingleton<Bootstrapper>();
    })
    // Configure Serilog *after* services are registered, allowing it to use them
    .UseSerilog((context, services, loggerConfiguration) =>
    {
        Infrastructure.Logging.SerilogSetup.Configure(services, loggerConfiguration);
    })
    .Build();

        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<LoadingWindow>();
            mainWindow.Show();

            var bootstrapper = _host.Services.GetRequiredService<Bootstrapper>();
            var appSettings = await bootstrapper.RunAsync();

            if (appSettings != null)
            {
                var browserWindow = _host.Services.GetRequiredService<BrowserWindow>();
                browserWindow.Title = appSettings.ServerSettings.AppName;
                Application.Current.MainWindow = browserWindow;

                if (appSettings.ServerSettings.LanuchInBackgroundOnlyEnabled == false)
                    browserWindow.Show();

                mainWindow.Close();

                await browserWindow.NavigateAsync(appSettings.ServerSettings.RootUrl);

            }
            else
            {
                MessageBox.Show("Failed to start the application. Please check logs for details.",
                    "Startup Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            var logger = _host.Services.GetRequiredService<ILogger<App>>();
            var processManager = _host.Services.GetRequiredService<IProcessManager>();
            try
            {

                await processManager.StopServer();

            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error occurred while trying to close the local server");

            }
            finally
            {
                using (_host)
                {

                    await _host.StopAsync(TimeSpan.FromSeconds(5));

                }
                base.OnExit(e);
            }


        }

    }

}
