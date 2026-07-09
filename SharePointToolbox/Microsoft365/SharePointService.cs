using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json;
using SharePointToolbox.Models;
using Microsoft.Extensions.Options;
using SharePointToolbox.Configuration;
using SharePointToolbox.Helpers;
using System.Threading.Tasks;
using System.Net.Http;

namespace SharePointToolbox.Microsoft365;

/// <summary>
/// Gestisce tutte le operazioni SharePoint.
/// </summary>
public class SharePointService : GraphClient
{
    // ===========================
    // Campi privati
    // ===========================

    private readonly AuthenticationService _authenticationService;

    // Configurazione SharePoint letta da appsettings.json
    private readonly SharePointOptions _options;

    // ===========================
    // Costruttore
    // ===========================

    public SharePointService(
        AuthenticationService authenticationService,
        IOptions<SharePointOptions> options)
        : base(authenticationService)
    {
        _authenticationService = authenticationService;
        _options = options.Value;
    }

    internal class DrivesResponse
    {
        public List<DriveItem> Value { get; set; } = [];
    }

    internal class DriveItem
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string WebUrl { get; set; } = "";
    }

    /// <summary>
    /// Restituisce tutte le Document Library del sito.
    /// </summary>
    public async Task<string> GetDrivesAsync()
    {
        using HttpClient client = CreateHttpClient();

        HttpResponseMessage response =
            await client.GetAsync(
                "https://graph.microsoft.com/v1.0/sites/giovanardieassociatistud.sharepoint.com:/sites/GSLDataRoom:/drives");

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Restituisce la prima Document Library del sito.
    /// </summary>
    public async Task<DocumentLibrary> GetDefaultDocumentLibraryAsync()
    {
        string json = await GetDrivesAsync();

        DrivesResponse? response =
            JsonSerializer.Deserialize<DrivesResponse>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (response == null || response.Value.Count == 0)
            throw new Exception("Nessuna Document Library trouvata.");

        DriveItem drive = response.Value[0];

        return new DocumentLibrary
        {
            Id = drive.Id,
            Name = drive.Name,
            WebUrl = drive.WebUrl
        };
    }

    /// <summary>
    /// Crea una cartella nella Document Library. Se esiste già, accoda un numero progressivo e lo logga a schermo.
    /// </summary>
    /// <returns>Il nome della cartella effettivamente creata (comprensivo di eventuale numerazione)</returns>
    public async Task<string> CreateFolderAsync(string folderName)
    {
        // 1. Pulisce il nome originale dai caratteri vietati
        string safeFolderName = SanitizeFolderName(folderName);

        // 2. Recupera la Document Library di destinazione
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();

        using HttpClient client = new();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _authenticationService.AccessToken);

        string currentFolderName = safeFolderName;
        int counter = 1;
        bool success = false;

        // Logghiamo l'inizio dell'operazione
        UiLogger.Info($"Richiesta creazione cartella: '{safeFolderName}'...");

        // Ciclo di tentativo creazione
        while (!success)
        {
            string json =
            $$"""
                {
                    "name": "{{currentFolderName}}",
                    "folder": {},
                    "@microsoft.graph.conflictBehavior": "fail"
                }
            """;

            using StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response =
                await client.PostAsync(
                    $"https://graph.microsoft.com/v1.0/drives/{library.Id}/root/children",
                    content);

            // Se il server risponde con 409 (Conflict), la cartella esiste già!
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Generiamo il nuovo nome con il progressivo
                currentFolderName = $"{safeFolderName} ({counter})";

                // NOTIFICA VISIVA NEL LOG: L'utente vedrà la ridenominazione in tempo reale!
                UiLogger.Info($"[Conflitto] Nome già esistente. Tentativo di ridenominazione in: '{currentFolderName}'");

                counter++;
            }
            else
            {
                // Se è un altro errore esplode normalmente, se è 201 (Created) usciamo dal ciclo
                response.EnsureSuccessStatusCode();
                success = true;
            }
        }

        // NOTIFICA DI SUCCESSO FINALE NEL LOG
        UiLogger.Info($"[Successo] Cartella creata correttamente come: '{currentFolderName}'");

        // Restituiamo il nome finale in modo che il Form sappia cosa è successo
        return currentFolderName;
    }

    /// <summary>
    /// Metodo di supporto per correggere e uniformare i nomi cartella secondo le specifiche Microsoft.
    /// </summary>
    private string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Nuova-Cartella"; // <-- Uniformato con il trattino '-' al posto di '_'

        // Lista ufficiale dei caratteri non ammessi da SharePoint Online e OneDrive
        char[] invalidChars = { '"', '*', ':', '<', '>', '?', '/', '\\', '|', '#', '%' };

        string sanitized = name;

        // Sostituisce i simboli non consentiti con il tuo trattino '-' scelto
        foreach (char c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '-');
        }

        // Rimuovespazi iniziali o finali (bloccati da SharePoint)
        sanitized = sanitized.Trim();

        // Rimuove eventuali tilde iniziali (riservate ai file temporanei Office)
        while (sanitized.StartsWith('~'))
        {
            sanitized = sanitized.Substring(1).Trim();
        }

        // Rimuove punti finali (non accettati dai sistemi Windows/SharePoint alla fine del nome)
        while (sanitized.EndsWith('.'))
        {
            sanitized = sanitized.TrimEnd('.').Trim();
        }

        // Se dopo la pulizia totale la stringa risulta vuota, assegna un nome di fallback valido
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Cartella-Valida"; // <-- Uniformato con il trattino '-' al posto di '_'
        }

        return sanitized;
    }
}