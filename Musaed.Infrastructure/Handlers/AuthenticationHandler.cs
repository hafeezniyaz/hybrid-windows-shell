using Musaed.Core.Interfaces;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Musaed.Infrastructure.Handlers;

/// <summary>
/// A DelegatingHandler that intercepts outgoing requests to add the JWT bearer token.
/// </summary>
public class AuthenticationHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthenticationHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the access token from our auth service.
        var token = await _authService.GetAccessTokenAsync();

        // If a token exists (i.e., we are in OAuth mode), add it to the header.
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // For Windows Auth, the token will be empty, and no header will be added.
        // The authentication will be handled by the underlying HttpClientHandler.

        // Continue sending the request down the pipeline.
        return await base.SendAsync(request, cancellationToken);
    }
}