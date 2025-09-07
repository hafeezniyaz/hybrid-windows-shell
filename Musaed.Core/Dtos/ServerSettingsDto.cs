using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Musaed.Core.Dtos
{
    /// <summary>
    /// Represents the complete set of dynamic settings fetched from the backend server.
    /// </summary>
    public class ServerSettingsDto
    {
        [JsonPropertyName("appName")]
        public required string AppName { get; set; }

        [JsonPropertyName("version")]
        public required string Version { get; set; }

        [JsonPropertyName("url")]
        public string? RootUrl { get; set; }

        [JsonPropertyName("continueInBackgroundEnabled")]
        public bool? ContinueInBackgroundEnabled { get; set; } = false;

        [JsonPropertyName("lanuchInBackgroundOnlyEnabled")]
        public bool? LanuchInBackgroundOnlyEnabled { get; set; } = false;

        [JsonPropertyName("exeName")]
        public required string ExeName { get; set; }

        [JsonPropertyName("healthApiPath")]
        public string? HealthApiPath { get; set; } = "/health";

        [JsonPropertyName("useWindowsAuth")]
        public bool? UseWindowsAuth { get; set; } = false;



    }
}
