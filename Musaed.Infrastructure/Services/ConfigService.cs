using Microsoft.Extensions.Configuration;
using Musaed.Core.Dtos;
using Musaed.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Musaed.Infrastructure.Services
{
    public class ConfigService : IConfigService
    {
        public AppConfig Config { get; }

        public ConfigService()
        {
            try
            {
                // Find the config file in the same directory as the executable
                var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
                var json = File.ReadAllText(configPath);

                // Deserialize the JSON into our AppConfig object.
                // The options object makes the deserializer case-insensitive, which is a good safety measure.
                Config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // A final check to ensure the file wasn't empty or malformed in a way that returns null.
                if (Config is null)
                {
                    throw new ApplicationException("Configuration file 'config.json' could not be parsed.");
                }
            }
            catch (FileNotFoundException ex)
            {
                throw new ApplicationException("CRITICAL: Configuration file 'config.json' was not found.", ex);
            }
            catch (JsonException ex)
            {
                throw new ApplicationException("CRITICAL: Configuration file 'config.json' is malformed.", ex);
            }
        }
    }
}
