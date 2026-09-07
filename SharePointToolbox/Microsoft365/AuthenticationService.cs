using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;
using SharePointToolbox.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SharePointToolbox.Microsoft365;

/// <summary>
/// Gestisce l'autenticazione verso Microsoft Entra ID.
/// </summary>
public class AuthenticationService
{
    private static readonly string[] Scopes =
    {
        "User.Read",
        "User.ReadBasic.All",
        "Files.ReadWrite.All",
        "Sites.ReadWrite.All",
        "Sites.Manage.All",
        "GroupMember.Read.All",
        "Mail.Send",
        "Mail.Send.Shared"
    };

    public record CurrentUserProfile(string Id, string DisplayName, string Email);
    public record DirectoryUserSuggestion(string DisplayName, string Email)
    {
        public override string ToString() => $"{DisplayName}  ·  {Email}";
    }
    // Contiene la configurazione letta da appsettings.json
    private readonly AzureAdOptions _options;

    // Oggetto MSAL che gestisce login e token
    private readonly IPublicClientApplication _msalApp;

    // Contiene il risultato dell'ultima autenticazione
    private AuthenticationResult? _authenticationResult;
    private MsalCacheHelper? _cacheHelper;
    private readonly Task _tokenCacheInitialization;
    private readonly SemaphoreSlim _authenticationLock = new(1, 1);

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

        _tokenCacheInitialization = InitializeTokenCacheAsync();
    }

    /// <summary>
    /// Apre la finestra Microsoft per autenticare l'utente.
    /// Restituisce il token di accesso se il login va a buon fine.
    /// </summary>
    public async Task<AuthenticationResult> SignInAsync()
    {
        await _authenticationLock.WaitAsync();
        try
        {
            await _tokenCacheInitialization;

            IAccount? account = (await _msalApp.GetAccountsAsync()).FirstOrDefault();
            if (account is not null)
            {
                try
                {
                    _authenticationResult = await _msalApp
                        .AcquireTokenSilent(Scopes, account)
                        .ExecuteAsync();
                    LastSignInWasSilent = true;
                    return _authenticationResult;
                }
                catch (MsalUiRequiredException)
                {
                    // Consenso, MFA o sessione scaduta richiedono l'interazione dell'utente.
                }
            }

            AcquireTokenInteractiveParameterBuilder interactiveRequest = _msalApp
                .AcquireTokenInteractive(Scopes);
            if (account is not null) interactiveRequest = interactiveRequest.WithAccount(account);

            _authenticationResult = await interactiveRequest.ExecuteAsync();
            LastSignInWasSilent = false;
            return _authenticationResult;
        }
        finally
        {
            _authenticationLock.Release();
        }
    }

    private async Task InitializeTokenCacheAsync()
    {
        string cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SharePointToolbox");
        Directory.CreateDirectory(cacheDirectory);

        StorageCreationProperties storageProperties = new StorageCreationPropertiesBuilder(
                "msalcache.bin3",
                cacheDirectory)
            .Build();
        _cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        _cacheHelper.RegisterCache(_msalApp.UserTokenCache);
    }

    /// Restituisce l'Access Token dell'utente autenticato.
    public string? AccessToken => _authenticationResult?.AccessToken;

    public async Task<string> GetAccessTokenAsync(
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        await _authenticationLock.WaitAsync(cancellationToken);
        try
        {
            await _tokenCacheInitialization;
            IAccount? account = (await _msalApp.GetAccountsAsync()).FirstOrDefault();
            if (account is null)
                throw new InvalidOperationException("La sessione Microsoft 365 non è attiva.");

            _authenticationResult = await _msalApp
                .AcquireTokenSilent(Scopes, account)
                .WithForceRefresh(forceRefresh)
                .ExecuteAsync(cancellationToken);
            return _authenticationResult.AccessToken;
        }
        finally
        {
            _authenticationLock.Release();
        }
    }

    public bool LastSignInWasSilent { get; private set; }

    /// Restituisce il provider di autenticazione MSAL.
    /// Verrà utilizzato da Microsoft Graph.
    public IPublicClientApplication MsalApp => _msalApp;

    /// <summary>
    /// Recupera i dati dell'utente autenticato tramite Microsoft Graph.
    /// </summary>
    public async Task<string> GetCurrentUserAsync()
    {
        using HttpClient client = CreateGraphClient();

        // Chiama Microsoft Graph
        HttpResponseMessage response =
            await client.GetAsync("https://graph.microsoft.com/v1.0/me");

        // Genera un'eccezione se la chiamata fallisce
        response.EnsureSuccessStatusCode();

        // Restituisce il JSON ricevuto
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<CurrentUserProfile> GetCurrentUserProfileAsync()
    {
        using HttpClient client = CreateGraphClient();
        HttpResponseMessage response = await client.GetAsync(
            "https://graph.microsoft.com/v1.0/me?$select=id,displayName,mail,userPrincipalName");
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = document.RootElement;
        string email = root.TryGetProperty("mail", out JsonElement mail) ? mail.GetString() ?? "" : "";
        if (string.IsNullOrWhiteSpace(email) && root.TryGetProperty("userPrincipalName", out JsonElement upn))
            email = upn.GetString() ?? "";

        return new CurrentUserProfile(
            root.GetProperty("id").GetString() ?? email,
            root.GetProperty("displayName").GetString() ?? email,
            email);
    }

    public async Task<IReadOnlyList<DirectoryUserSuggestion>> SearchInternalUsersAsync(
        string searchText,
        string internalDomain,
        CancellationToken cancellationToken = default)
    {
        string term = searchText.Trim();
        if (term.Length < 2) return Array.Empty<DirectoryUserSuggestion>();

        string escapedTerm = term
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
        string search =
            $"\"displayName:{escapedTerm}\" OR " +
            $"\"mail:{escapedTerm}\" OR " +
            $"\"userPrincipalName:{escapedTerm}\"";
        string url = "https://graph.microsoft.com/v1.0/users" +
                     "?$select=displayName,mail,userPrincipalName" +
                     $"&$search={Uri.EscapeDataString(search)}&$count=true&$top=12";

        using HttpClient client = CreateGraphClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("ConsistencyLevel", "eventual");
        HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        string domainSuffix = internalDomain.StartsWith('@') ? internalDomain : $"@{internalDomain}";
        return document.RootElement.GetProperty("value").EnumerateArray()
            .Select(user =>
            {
                string displayName = user.TryGetProperty("displayName", out JsonElement display)
                    ? display.GetString() ?? string.Empty
                    : string.Empty;
                string mail = user.TryGetProperty("mail", out JsonElement mailElement)
                    ? mailElement.GetString() ?? string.Empty
                    : string.Empty;
                if (string.IsNullOrWhiteSpace(mail) && user.TryGetProperty("userPrincipalName", out JsonElement upn))
                    mail = upn.GetString() ?? string.Empty;
                return new DirectoryUserSuggestion(displayName, mail);
            })
            .Where(user => !string.IsNullOrWhiteSpace(user.Email)
                           && !user.Email.Contains("#EXT#", StringComparison.OrdinalIgnoreCase)
                           && user.Email.EndsWith(domainSuffix, StringComparison.OrdinalIgnoreCase))
            .DistinctBy(user => user.Email, StringComparer.OrdinalIgnoreCase)
            .OrderBy(user => user.DisplayName)
            .Take(8)
            .ToList();
    }

    public async Task<bool> IsCurrentUserMemberOfGroupAsync(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId)) return false;

        using HttpClient client = CreateGraphClient();
        using StringContent content = new(
            JsonSerializer.Serialize(new { groupIds = new[] { groupId } }),
            System.Text.Encoding.UTF8,
            "application/json");
        HttpResponseMessage response = await client.PostAsync(
            "https://graph.microsoft.com/v1.0/me/checkMemberGroups",
            content);
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("value").EnumerateArray()
            .Any(value => value.GetString()?.Equals(groupId, StringComparison.OrdinalIgnoreCase) == true);
    }

    private HttpClient CreateGraphClient()
    {
        string token = AccessToken
            ?? throw new InvalidOperationException("La sessione Microsoft 365 non è attiva.");
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(AppConstants.GraphRequestTimeoutSeconds)
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

}

