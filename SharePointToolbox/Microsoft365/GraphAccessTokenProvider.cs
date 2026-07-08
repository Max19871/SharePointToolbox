using Microsoft.Kiota.Abstractions.Authentication;
using SharePointToolbox.Microsoft365;

namespace SharePointToolbox.Graph;

/// <summary>
/// Fornisce a Microsoft Graph l'Access Token ottenuto tramite MSAL.
/// </summary>
public class GraphAccessTokenProvider : IAccessTokenProvider
{
    // ===========================
    // Private fields
    // ===========================

    private readonly AuthenticationService _authenticationService;

    // ===========================
    // Constructor
    // ===========================

    public GraphAccessTokenProvider(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    // ===========================
    // Public properties
    // ===========================

    public AllowedHostsValidator AllowedHostsValidator { get; } = new();

    // ===========================
    // Public methods
    // ===========================

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_authenticationService.AccessToken ?? string.Empty);
    }
}