using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Musaed.Core.Dtos
{
    public  class TokenResponseDto
    {

        // The [JsonPropertyName] attribute maps the JSON field "accessToken" (or whatever the
        // server sends) to our C# property "AccessToken". This is a best practice for handling
        // different naming conventions.
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresInSeconds { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}

