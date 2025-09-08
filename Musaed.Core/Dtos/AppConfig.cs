namespace Musaed.Core.Dtos;

/// <summary>
/// Represents the application's configuration settings, read from config.json.
/// A simple "POCO" (Plain Old C# Object) used to hold data.
/// </summary>
public class AppConfig
{
    public  required string BackendRootUrl { get; set; } = string.Empty;
    public string AppName { get; set; } = string.Empty;

    public string  ConfigName { get; set; }

    public string AssetName { get; set; }


    public string AuthMode { get; set; } // "windows" or "oauth2"

    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string PackageDownloadRootFolder { get; set; } // e.g., "%LOCALAPPDATA%/Musaed"

    public bool? LogToOrchestrator { get; set; } = true;
}