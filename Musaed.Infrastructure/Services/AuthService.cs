using Microsoft.Extensions.Logging;
using Musaed.Core.Dtos;
using Musaed.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Musaed.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IConfigService _configService;
    private readonly HttpClient _httpClient;

    // In-memory cache for the access token and its expiry time
    private string? _cachedAccessToken;
    private DateTime _tokenExpiryTime = DateTime.MinValue;

    public AuthService(ILogger<AuthService> logger, IConfigService configService, HttpClient httpClient)
    {
        _logger = logger;
        _configService = configService;
        _httpClient = httpClient;
    }

    public async Task<string> GetAccessTokenAsync()
    {
        var authMode = _configService.Config.AuthMode;

        // If mode is "windows", we don't need a token. Return empty string.
        if ("windows".Equals(authMode, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Using Windows Authentication. No bearer token required.");
            return string.Empty;
        }

        // If mode is not "windows", it must be oauth2.
        _logger.LogInformation("Using OAuth2 authentication.");

        // Check if we have a valid, non-expired token in memory
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiryTime)
        {
            _logger.LogInformation("Returning cached access token.");
            return _cachedAccessToken;
        }

        // Fetch a new token from the server
        _logger.LogInformation("Fetching new OAuth token from server.");
        try
        {
            var tokenEndpoint = new Uri(new Uri(_configService.Config.BackendRootUrl), "connect/token");

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", _configService.Config.ClientId!),
                new KeyValuePair<string, string>("client_secret", _configService.Config.ClientSecret!)
            });

            var response = await _httpClient.PostAsync(tokenEndpoint, requestBody);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
            if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                throw new AuthenticationException("Received an empty or invalid token from the server.");
            }

            // Cache the new token and its expiry (with a 60-second safety buffer)
            _cachedAccessToken = tokenResponse.AccessToken;
            _tokenExpiryTime = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds - 60);

            _logger.LogInformation("Successfully fetched and cached new access token.");
            return _cachedAccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve OAuth access token.");
            throw new AuthenticationException("Authentication failed. See inner exception for details.", ex);
        }
    }
}