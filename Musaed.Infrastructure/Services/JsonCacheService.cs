using Musaed.Core.Interfaces;
using System;
using System.IO;
using System.Text.Json;

namespace Musaed.Infrastructure.Services;

public class JsonCacheService : ICacheService
{
    private readonly string _cacheDirectory;

    public JsonCacheService(IConfigService configService)
    {
        // Expand the environment variable from config to get the full path
        var rootPath = Environment.ExpandEnvironmentVariables(configService.Config.PackageDownloadRootFolder);
        _cacheDirectory = Path.Combine(rootPath, "Cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public void Save<T>(T data, string key)
    {
        var filePath = Path.Combine(_cacheDirectory, $"{key}.json");
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public T Load<T>(string key)
    {
        var filePath = Path.Combine(_cacheDirectory, $"{key}.json");
        if (!File.Exists(filePath))
        {
            return default; // Returns null for reference types
        }
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(json);
    }
}