using Microsoft.Extensions.DependencyInjection;
using Musaed.Core.Interfaces;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.PeriodicBatching;
using System;
using System.IO;
using System.Net.Http;

namespace Musaed.Infrastructure.Logging;

/// <summary>
/// A static class to encapsulate the Serilog configuration logic.
/// </summary>
public static class SerilogSetup
{
    /// <summary>
    /// Configures Serilog for the application, with access to the DI container.
    /// </summary>
    public static void Configure(IServiceProvider services, LoggerConfiguration loggerConfiguration)
    {
        // Resolve services from the DI container.
        var configService = services.GetRequiredService<IConfigService>();
        var appConfig = configService.Config;
        var appName = appConfig.AppName ?? "Musaed";

        var logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "Logs");

        Directory.CreateDirectory(logDirectory); // Ensure the directory exists

        var logPath = Path.Combine(logDirectory, "log-.json");

        loggerConfiguration
            .Enrich.FromLogContext()
            .MinimumLevel.Debug()
            .WriteTo.File(
                new JsonFormatter(),
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7);

        // Conditionally add the custom HTTP sink if configured to do so.
        if (appConfig is { LogToOrchestrator: true, BackendRootUrl: not null })
        {
            // Get the factory and create a client with the correct handlers (auth, retry).
            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("LoggingClient");

            var logApiEndpoint = new Uri(new Uri(appConfig.BackendRootUrl), "v1/api/logs");
            var bufferFilePath = Path.Combine(logDirectory, "log-buffer.json");

            var httpSink = new HttpSink(logApiEndpoint.ToString(), appName, httpClient, bufferFilePath);

            var batchingOptions = new PeriodicBatchingSinkOptions
            {
                BatchSizeLimit = 50, // Send logs in batches of 50
                Period = TimeSpan.FromSeconds(5), // Or every 5 seconds, whichever comes first
                EagerlyEmitFirstEvent = true
            };

            var batchingSink = new PeriodicBatchingSink(httpSink, batchingOptions);

            loggerConfiguration.WriteTo.Sink(batchingSink);
        }
    }
}

