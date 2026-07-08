using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using SharePointToolbox.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SharePointToolbox.Microsoft365;

/// <summary>
/// Gestisce l'autenticazione verso Microsoft Entra ID.
/// </summary>
public class AuthenticationService
{
    // Contiene la configurazione letta da appsettings.json
    private readonly AzureAdOptions _options;

    // Oggetto MSAL che gestisce login e token
    private readonly IPublicClientApplication _msalApp;

    // Contiene il risultato dell'ultima autenticazione
    private AuthenticationResult? _authenticationResult;

    public AuthenticationService(IOptions<AzureAdOptions> options)
    {
        _options = options.Value;

        // Configura il client MSAL utilizzando il Redirect URI predefinito
        // per le applicazioni desktop.
        _msalApp = PublicClientApplicationBuilder
            .Create(_options.ClientId)
            .WithTenantId(_options.TenantId)
            .WithDefaultRedirectUri()
            .Build();
    }

    /// <summary>
    /// Apre la finestra Microsoft per autenticare l'utente.
    /// Restituisce il token di accesso se il login va a buon fine.
    /// </summary>
    public async Task<AuthenticationResult> SignInAsync()
    {
        // Permessi richiesti all'utente
        string[] scopes =
        {
        "User.Read",
        "Files.ReadWrite.All",
        "Sites.ReadWrite.All"
    };

        // Avvia il login interattivo
        _authenticationResult = await _msalApp
            .AcquireTokenInteractive(scopes)
            .ExecuteAsync();

        return _authenticationResult;
    }

    /// Restituisce l'Access Token dell'utente autenticato.
    public string? AccessToken => _authenticationResult?.AccessToken;

    /// Restituisce il provider di autenticazione MSAL.
    /// Verrà utilizzato da Microsoft Graph.
    public IPublicClientApplication MsalApp => _msalApp;

    /// <summary>
    /// Recupera i dati dell'utente autenticato tramite Microsoft Graph.
    /// </summary>
    public async Task<string> GetCurrentUserAsync()
    {
        // Crea un client HTTP
        using HttpClient client = new();

        // Inserisce il Bearer Token ottenuto dal login
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AccessToken);

        // Chiama Microsoft Graph
        HttpResponseMessage response =
            await client.GetAsync("https://graph.microsoft.com/v1.0/me");

        // Genera un'eccezione se la chiamata fallisce
        response.EnsureSuccessStatusCode();

        // Restituisce il JSON ricevuto
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Recupera le informazioni del sito SharePoint.
    /// </summary>
    public async Task<string> GetSiteAsync()
    {
        using HttpClient client = new();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AccessToken);

        HttpResponseMessage response =
            await client.GetAsync(
                "https://graph.microsoft.com/v1.0/sites/giovanardieassociatistud.sharepoint.com:/sites/GSLDataRoom");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

}

