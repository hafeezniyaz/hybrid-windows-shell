using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Musaed.Core.Dtos
{
    public  class AppSettingsDto
    {
        [JsonPropertyName("serverSettings")]
        public ServerSettingsDto ServerSettings { get; set; }

        public string CredentialName { get; set; }
    }
}
