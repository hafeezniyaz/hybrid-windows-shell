using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.IO;

namespace Musaed.Infrastructure.Logging;

/// <summary>
/// A static class to encapsulate the Serilog configuration logic.
/// </summary>
public static class SerilogSetup
{
    /// <summary>
    /// An extension method on IHostBuilder to configure Serilog for our application.
    /// </summary>
    public static IHostBuilder UseSerilogForApp(this IHostBuilder builder)
    {
        // Get the application name from a shared location (e.g., assembly name)
        var appName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Musaed";

        // Define the path for the logs in the user's local app data folder.
        // This is the standard location for application-specific data.
        // Example: C:\Users\YourUser\AppData\Local\Musaed\Logs\log-20250825.json
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName,
            "Logs",
            "log-.json");

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext() // Adds contextual information to log events
            .MinimumLevel.Debug() // Capture all log levels from Debug upwards
            .WriteTo.File(
                new JsonFormatter(), // Write logs in JSON format
                logPath,
                rollingInterval: RollingInterval.Day, // Create a new log file each day
                retainedFileCountLimit: 7) // Keep the last 7 days of logs
            .CreateLogger();

        // Tell the HostBuilder to use Serilog for all its logging needs.
        builder.UseSerilog();

        return builder;
    }
}