using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using SharePointToolbox.Configuration;

namespace SharePointToolbox.Microsoft365;

/// <summary>
/// Classe base per i servizi Microsoft Graph.
/// </summary>
public abstract class GraphClient
{
    private readonly AuthenticationService _authenticationService;

    protected GraphClient(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <summary>
    /// Crea un HttpClient già autenticato.
    /// </summary>
    protected HttpClient CreateHttpClient()
    {
        HttpClient client = new()
        {
            Timeout = TimeSpan.FromSeconds(AppConstants.GraphRequestTimeoutSeconds)
        };

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _authenticationService.AccessToken);

        return client;
    }
}
