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
            _host = Host.CreateDefaultBuilder() // 1. Create the generic host builder
                .UseSerilogForApp()              // 2. Plug in our Serilog configuration
                .ConfigureServices((context, services) =>
                {
                    // 3. Register our services with the DI container

                    // We register ConfigService as the implementation for IConfigService.
                    // AddSingleton means only one instance will be created for the entire application lifetime.
                    services.AddSingleton<IConfigService, ConfigService>();

                    // --- Authentication ---
                    // Register our custom handler. It's transient because it's part of the HTTP pipeline.
                    services.AddTransient<AuthenticationHandler>();

                    // Register the AuthService and give it a simple, dedicated HttpClient.
                    services.AddHttpClient<IAuthService, AuthService>((serviceProvider, client) =>
                    {
                        var configService = serviceProvider.GetRequiredService<IConfigService>();
                        client.BaseAddress = new Uri(configService.Config.BackendRootUrl);
                    });


                    // This configures the IHttpClientFactory and adds our BackendApiClient.
                    // It's the modern, recommended way to use HttpClient in .NET.
                    // --- Main API Client ---
                    services.AddHttpClient<IBackendApiClient, BackendApiClient>((serviceProvider, client) =>
                    {
                        var configService = serviceProvider.GetRequiredService<IConfigService>();
                        client.BaseAddress = new Uri(configService.Config.BackendRootUrl);
                    })
                    // This is the corrected, conditional configuration for the primary handler.
                    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                    {
                        // First, get the configuration service.
                        var configService = serviceProvider.GetRequiredService<IConfigService>();
                        var handler = new HttpClientHandler();

                        // Check the AuthMode from the configuration.
                        if ("windows".Equals(configService.Config.AuthMode, StringComparison.OrdinalIgnoreCase))
                        {
                            // ONLY apply this setting if the mode is "windows".
                            handler.UseDefaultCredentials = true;
                        }

                        // For "oauth" mode, UseDefaultCredentials remains false, which is correct.
                        return handler;
                    })
                    .AddHttpMessageHandler<AuthenticationHandler>()
                                        // This adds a Polly resilience policy.
                                        // It will automatically retry a failed request up to 3 times with a delay.
                                        .AddPolicyHandler((serviceProvider, request) =>
                    {
                        // Get the ILoggerFactory from the service provider
                        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                        // Create a logger with a specific category for our retry policy
                        var logger = loggerFactory.CreateLogger($"PollyRetryPolicy.{request.Method.Method}");

                        // Manually define the policy that the old helper used to create for us
                        return HttpPolicyExtensions
                            .HandleTransientHttpError() // This handles the same 5xx, 408, etc. errors
                            .WaitAndRetryAsync(
                                3, // The total number of retries
                                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                                onRetry: (outcome, timespan, retryAttempt, context) => // This block now uses our real logger
                                {
                                    logger.LogWarning(
                                        "Request to {RequestUri} failed with {StatusCode}. Waiting {TimeSpan} before next retry. Retry attempt {RetryAttempt}",
                                        request.RequestUri,
                                        outcome.Result?.StatusCode,
                                        timespan,
                                        retryAttempt);
                                }
                            );
                    });

                    services.AddSingleton<ICacheService, JsonCacheService>();
                    services.AddSingleton<IPackageManager, PackageManagerService>();
                    services.AddSingleton<IProcessManager, ProcessManagerService>();
                    // Register the main window of our application
                    services.AddSingleton<LoadingViewModel>();
                    services.AddTransient<LoadingWindow>();
                    services.AddTransient<BrowserWindow>();
                    services.AddSingleton<Bootstrapper>();
                })
                .Build(); // 4. Build the host
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
