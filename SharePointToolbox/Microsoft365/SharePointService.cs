using System;
using System.Collections.Generic;
using System.Text;
using System.Linq; // FONDAMENTALE per .FirstOrDefault()
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SharePointToolbox.Models;
using SharePointToolbox.Configuration;
using SharePointToolbox.Helpers;
using Microsoft.Extensions.Options;

namespace SharePointToolbox.Microsoft365;

/// <summary>
/// Gestisce tutte le operazioni SharePoint.
/// Questo file contiene SOLO la logica di comunicazione API.
/// </summary>
public class SharePointService : GraphClient
{
    private readonly SharePointOptions _options;
    private readonly MailOptions _mailOptions;
    private readonly ThemeOptions _themeOptions;
    private Dictionary<string, string>? _metadataColumnNames;
    private string? _siteId;
    private string? _acceptanceListId;
    private string? _activityListId;
    private DocumentLibrary? _defaultDocumentLibrary;
    private readonly SemaphoreSlim _documentLibraryLock = new(1, 1);
    private readonly SemaphoreSlim _metadataColumnsLock = new(1, 1);

    public record FolderMetadata(string RequestedBy, string Notes, DateTimeOffset? ExpirationDate);
    public record PracticeClosureResult(IReadOnlyList<string> RevokedUsers, IReadOnlyList<string> Errors);
    public record AuditActivity(
        DateTimeOffset Timestamp,
        string UserName,
        string Email,
        string Operation,
        string PracticeName,
        string Outcome,
        string Recipient,
        string Details);
    public record RecentSharePointActivity(
        DateTimeOffset Timestamp,
        string User,
        string Operation,
        string ItemName,
        string ItemType,
        string Details,
        string Path,
        string ItemId);
    public record DisclaimerAcceptance(
        DateTimeOffset Timestamp,
        string UserName,
        string Email,
        string DisclaimerVersion,
        string ApplicationVersion,
        string Outcome,
        string TextHash);

    public SharePointService(
        AuthenticationService authenticationService,
        IOptions<SharePointOptions> options,
        IOptions<MailOptions> mailOptions,
        IOptions<ThemeOptions> themeOptions)
        : base(authenticationService)
    {
        _options = options.Value;
        _mailOptions = mailOptions.Value;
        _themeOptions = themeOptions.Value;
    }

    public class DrivesResponse
    {
        public List<DriveItem> Value { get; set; } = new();

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }

    public class DriveItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string WebUrl { get; set; } = "";
        public DateTimeOffset? CreatedDateTime { get; set; }
        public IdentitySet? CreatedBy { get; set; }
        public FolderFacet? Folder { get; set; }
    }

    public sealed class FolderFacet { }

    public class IdentitySet
    {
        public Identity? User { get; set; }
    }

    public class Identity
    {
        public string DisplayName { get; set; } = "";
    }

    // --- METODI API ---

    public async Task<string> GetDrivesAsync()
    {
        string siteId = await GetSiteIdAsync();
        using HttpClient client = CreateHttpClient();
        HttpResponseMessage response = await client.GetAsync(
            $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/drives");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<DocumentLibrary> GetDefaultDocumentLibraryAsync()
    {
        if (_defaultDocumentLibrary is not null) return _defaultDocumentLibrary;

        await _documentLibraryLock.WaitAsync();
        try
        {
            if (_defaultDocumentLibrary is not null) return _defaultDocumentLibrary;

            string json = await GetDrivesAsync();
            var response = JsonSerializer.Deserialize<DrivesResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null || response.Value.Count == 0)
                throw new InvalidOperationException("Nessuna raccolta documenti trovata nel sito SharePoint configurato.");

            DriveItem drive = response.Value[0];
            _defaultDocumentLibrary = new DocumentLibrary { Id = drive.Id, Name = drive.Name, WebUrl = drive.WebUrl };
            return _defaultDocumentLibrary;
        }
        finally
        {
            _documentLibraryLock.Release();
        }
    }

    /// <summary>
    /// Metodo corretto per aggiungere permessi senza forzare inviti (che causerebbero errori con gli alias +)
    /// </summary>
    public async Task AddPermissionAsync(string itemId, string email, string role, bool isExternal, bool preventDownload, string message)
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();

        string url = $"https://graph.microsoft.com/v1.0/drives/{library.Id}/items/{itemId}/invite";

        var body = new Dictionary<string, object>
        {
            { "recipients", new[] { new { email = email } } },
            { "roles", new[] { role } },
            { "requireSignIn", true },
            // L'email viene inviata separatamente dalla casella condivisa configurata,
            // così il mittente è uniforme per tutti gli operatori.
            { "sendInvitation", false }
        };

        if (!string.IsNullOrWhiteSpace(message))
        {
            body.Add("message", message);
        }

        // Aggiungiamo il flag di blocco download solo se non è esterno (per evitare conflitti)
        if (!isExternal && preventDownload)
        {
            body.Add("preventsDownload", true);
        }

        string json = JsonSerializer.Serialize(body);
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            using HttpResponseMessage response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
                return;

            string error = await response.Content.ReadAsStringAsync();
            bool transientSharingFailure =
                response.StatusCode == System.Net.HttpStatusCode.BadRequest
                && error.Contains("sharingFailed", StringComparison.OrdinalIgnoreCase);
            if (transientSharingFailure && attempt < 3)
            {
                await Task.Delay(attempt * 750);
                continue;
            }

            if (transientSharingFailure)
            {
                throw new InvalidOperationException(
                    "SharePoint non è riuscito ad assegnare l’accesso dopo più tentativi. "
                    + "Verificare l’indirizzo del destinatario e riprovare tra qualche minuto.");
            }

            throw new InvalidOperationException(
                $"SharePoint non ha accettato la richiesta ({response.StatusCode}).");
        }
    }

    public async Task RemovePermissionAsync(string itemId, string permissionId)
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();
        string url = $"https://graph.microsoft.com/v1.0/drives/{library.Id}/items/{itemId}/permissions/{permissionId}";
        HttpResponseMessage response = await client.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<PermissionEntry>> GetPermissionsAsync(string itemId)
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();
        string url = $"https://graph.microsoft.com/v1.0/drives/{library.Id}/items/{itemId}/permissions";
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync();
        return ParsePermissionsFromJson(json);
    }

    private static List<PermissionEntry> ParsePermissionsFromJson(string json)
    {
        var entries = new List<PermissionEntry>();
        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("value", out JsonElement permissions)) return entries;

        foreach (JsonElement permission in permissions.EnumerateArray())
        {
            string id = GetString(permission, "id");
            string role = permission.TryGetProperty("roles", out JsonElement roles)
                ? roles.EnumerateArray().FirstOrDefault().GetString() ?? "read"
                : "read";
            int initialCount = entries.Count;

            if (permission.TryGetProperty("grantedToV2", out JsonElement grantedTo))
                AddIdentityEntries(entries, grantedTo, id, role);

            if (permission.TryGetProperty("grantedToIdentitiesV2", out JsonElement identities))
                foreach (JsonElement identity in identities.EnumerateArray())
                    AddIdentityEntries(entries, identity, id, role);

            if (entries.Count == initialCount && permission.TryGetProperty("link", out JsonElement link))
                entries.Add(new PermissionEntry
                {
                    Id = id,
                    Role = role,
                    DisplayName = $"Collegamento {GetString(link, "scope")}".Trim(),
                    Email = "-",
                    IsExternal = true
                });
        }

        return entries
            .DistinctBy(entry => new
            {
                entry.Id,
                Identity = !string.IsNullOrWhiteSpace(entry.Email) && entry.Email != "-"
                    ? entry.Email.ToUpperInvariant()
                    : entry.DisplayName.ToUpperInvariant(),
                entry.Role
            })
            .ToList();
    }

    public async Task SendPracticeLinkEmailAsync(
        string recipient,
        string practiceName,
        string practiceUrl,
        string? customMessage = null)
    {
        if (string.IsNullOrWhiteSpace(_mailOptions.SharedMailboxAddress))
            throw new InvalidOperationException("La casella condivisa utilizzata per gli invii non è configurata.");

        using HttpClient client = CreateHttpClient();
        string templatePath = ResolveMailResourcePath(_mailOptions.TemplatePath);
        string logoPath = ResolveMailResourcePath(_mailOptions.LogoPath);
        string watermarkPath = ResolveMailResourcePath(_mailOptions.WatermarkPath);
        EnsureMailResourceExists(templatePath, "modello HTML");
        EnsureMailResourceExists(logoPath, "logo");
        EnsureMailResourceExists(watermarkPath, "filigrana");

        string subject = _mailOptions.SubjectTemplate
            .Replace("{Condivisione}", practiceName, StringComparison.OrdinalIgnoreCase)
            .Replace("\r", " ")
            .Replace("\n", " ");
        string html = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
        string customMessageBlock = string.IsNullOrWhiteSpace(customMessage)
            ? string.Empty
            : $"<div style=\"margin:20px 0;padding:14px 16px;background:#f5f7fa;border-left:4px solid {SafeHtmlColor(_themeOptions.InstitutionalAccent, "#C9B76A")};font-size:14px;line-height:21px;color:#444444;\">{EncodeMultiline(customMessage)}</div>";
        string confidentialityBlock = $"""
            <div style="height:1px;margin:28px 0 24px;background:{SafeHtmlColor(_themeOptions.InstitutionalAccent, "#C9B76A")};font-size:0;line-height:0;">&nbsp;</div>
            <p style="margin:0 0 8px;font-size:16px;font-weight:600;color:{SafeHtmlColor(_themeOptions.PrimaryDark, "#3E5885")};">{Encode(_mailOptions.ConfidentialityTitle)}</p>
            <p style="margin:0;font-size:12px;line-height:19px;color:#555555;">{Encode(_mailOptions.ConfidentialityText)}</p>
            """;

        var tokens = new Dictionary<string, string>
        {
            ["Subject"] = Encode(subject),
            ["ProductLabel"] = Encode(_mailOptions.ProductLabel),
            ["Introduction"] = Encode(_mailOptions.Introduction),
            ["Condivisione"] = Encode(practiceName),
            ["CustomMessageBlock"] = customMessageBlock,
            ["LinkCondivisione"] = Encode(practiceUrl),
            ["ButtonText"] = Encode(_mailOptions.ButtonText),
            ["AccessNotice"] = Encode(_mailOptions.AccessNotice),
            ["ConfidentialityBlock"] = confidentialityBlock,
            ["OfficeTitle"] = Encode(_mailOptions.OfficeTitle),
            ["OfficeAddress"] = Encode(_mailOptions.OfficeAddress),
            ["OfficePhone"] = Encode(_mailOptions.OfficePhone),
            ["SupportText"] = Encode(_mailOptions.SupportText),
            ["SupportAddress"] = Encode(_mailOptions.SupportAddress),
            ["PrimaryColor"] = SafeHtmlColor(_themeOptions.Primary, "#3E5885"),
            ["PrimaryDarkColor"] = SafeHtmlColor(_themeOptions.PrimaryDark, "#3E5885"),
            ["InstitutionalAccent"] = SafeHtmlColor(_themeOptions.InstitutionalAccent, "#C9B76A")
        };
        foreach ((string token, string value) in tokens)
            html = html.Replace($"{{{{{token}}}}}", value, StringComparison.Ordinal);

        object[] attachments =
        {
            CreateInlineImageAttachment(logoPath, "studio-logo", "logo-studio.png"),
            CreateInlineImageAttachment(watermarkPath, "building-watermark", "filigrana-palazzo.png")
        };
        var body = new
        {
            message = new
            {
                subject,
                body = new
                {
                    contentType = "HTML",
                    content = html
                },
                from = new
                {
                    emailAddress = new { address = _mailOptions.SharedMailboxAddress.Trim() }
                },
                toRecipients = new[] { new { emailAddress = new { address = recipient } } },
                attachments
            },
            saveToSentItems = true
        };
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(
            "https://graph.microsoft.com/v1.0/me/sendMail",
            content);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Invio email non riuscito ({response.StatusCode}): {error}");
        }
    }

    public async Task SendCreationNotificationEmailAsync(
        AuthenticationService.CurrentUserProfile createdBy,
        string sharingName,
        string requestedBy,
        string notes,
        DateTimeOffset? expirationDate,
        DateTimeOffset createdDate,
        string sharingUrl)
    {
        if (string.IsNullOrWhiteSpace(_mailOptions.CreationNotificationAddress))
            throw new InvalidOperationException(
                "L’indirizzo per le notifiche di creazione non è configurato.");
        if (string.IsNullOrWhiteSpace(_mailOptions.SharedMailboxAddress))
            throw new InvalidOperationException(
                "La casella condivisa utilizzata per gli invii non è configurata.");

        string subject = _mailOptions.CreationNotificationSubject
            .Replace("{Condivisione}", sharingName, StringComparison.OrdinalIgnoreCase)
            .Replace("\r", " ")
            .Replace("\n", " ");
        string primary = SafeHtmlColor(_themeOptions.Primary, "#3E5885");
        string accent = SafeHtmlColor(_themeOptions.InstitutionalAccent, "#C9B76A");
        string expiration = expirationDate?.LocalDateTime.ToString("dd/MM/yyyy") ?? "Nessuna";
        string noteValue = string.IsNullOrWhiteSpace(notes) ? "-" : notes;
        string requestedByValue = string.IsNullOrWhiteSpace(requestedBy) ? "-" : requestedBy;

        string html = $"""
            <!doctype html>
            <html lang="it">
            <body style="margin:0;padding:24px;background:#f4f5f7;font-family:Calibri,Arial,sans-serif;color:#333333;">
              <div style="max-width:680px;margin:0 auto;background:#ffffff;border-top:4px solid {accent};">
                <div style="padding:24px 28px;">
                  <div style="font-size:21px;font-weight:700;color:{primary};margin-bottom:6px;">
                    Nuova condivisione Data Room
                  </div>
                  <div style="font-size:14px;color:#666666;margin-bottom:24px;">
                    Notifica automatica generata dall’applicazione.
                  </div>
                  <table role="presentation" width="100%" style="width:100%;table-layout:fixed;border-collapse:collapse;font-size:15px;line-height:21px;">
                    {NotificationRow("Nome condivisione", sharingName)}
                    {NotificationRow("Creata da", createdBy.DisplayName)}
                    {NotificationRow("Account", createdBy.Email)}
                    {NotificationRow("Data e ora", createdDate.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss"))}
                    {NotificationRow("Richiesto da", requestedByValue)}
                    {NotificationRow("Note", noteValue, multiline: true)}
                    {NotificationRow("Scadenza", expiration)}
                    {NotificationLinkRow("Collegamento SharePoint", sharingUrl)}
                  </table>
                </div>
              </div>
            </body>
            </html>
            """;

        var body = new
        {
            message = new
            {
                subject,
                body = new { contentType = "HTML", content = html },
                from = new
                {
                    emailAddress = new { address = _mailOptions.SharedMailboxAddress.Trim() }
                },
                toRecipients = new[]
                {
                    new
                    {
                        emailAddress = new
                        {
                            address = _mailOptions.CreationNotificationAddress.Trim()
                        }
                    }
                }
            },
            saveToSentItems = true
        };

        using HttpClient client = CreateHttpClient();
        using var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        HttpResponseMessage response = await client.PostAsync(
            "https://graph.microsoft.com/v1.0/me/sendMail",
            content);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Invio notifica di creazione non riuscito ({response.StatusCode}): {error}");
        }
    }

    private static string NotificationRow(string label, string value, bool multiline = false)
    {
        string encodedValue = multiline ? EncodeMultiline(value) : Encode(value);
        return $"""
            <tr>
              <td width="175" style="width:175px;padding:7px 12px 7px 0;vertical-align:top;font-weight:700;color:#3E5885;">
                {Encode(label)}
              </td>
              <td style="padding:7px 0;vertical-align:top;border-bottom:1px solid #eceef1;word-break:break-word;overflow-wrap:anywhere;">
                {encodedValue}
              </td>
            </tr>
            """;
    }

    private static string NotificationLinkRow(string label, string url) =>
        $"""
        <tr>
          <td width="175" style="width:175px;padding:7px 12px 7px 0;vertical-align:top;font-weight:700;color:#3E5885;">
            {Encode(label)}
          </td>
          <td style="padding:7px 0;vertical-align:top;border-bottom:1px solid #eceef1;">
            <a href="{Encode(url)}" style="color:#3E5885;font-weight:700;text-decoration:underline;">
              Apri la condivisione
            </a>
          </td>
        </tr>
        """;

    private static string ResolveMailResourcePath(string configuredPath) =>
        Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

    private static void EnsureMailResourceExists(string path, string description)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"Il {description} della mail non è stato trovato: {path}");
    }

    private static Dictionary<string, object> CreateInlineImageAttachment(
        string path,
        string contentId,
        string name) =>
        new()
        {
            ["@odata.type"] = "#microsoft.graph.fileAttachment",
            ["name"] = name,
            ["contentType"] = "image/png",
            ["contentBytes"] = Convert.ToBase64String(File.ReadAllBytes(path)),
            ["isInline"] = true,
            ["contentId"] = contentId
        };

    private static string Encode(string? value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static string EncodeMultiline(string value) =>
        Encode(value.Trim()).Replace("\r\n", "<br>").Replace("\n", "<br>");

    private static string SafeHtmlColor(string? value, string fallback) =>
        value is not null && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$")
            ? value
            : fallback;

    private static void AddIdentityEntries(List<PermissionEntry> entries, JsonElement identitySet, string permissionId, string role)
    {
        foreach (string identityType in new[] { "user", "siteUser", "group", "siteGroup", "application" })
        {
            if (!identitySet.TryGetProperty(identityType, out JsonElement identity)) continue;

            string displayName = GetString(identity, "displayName");
            string loginName = NormalizeLoginName(GetString(identity, "loginName"));
            string email = FirstNotEmpty(
                GetString(identity, "email"),
                GetString(identity, "userPrincipalName"),
                loginName.Contains('@') ? loginName : string.Empty);

            entries.Add(new PermissionEntry
            {
                Id = permissionId,
                Role = role,
                DisplayName = FirstNotEmpty(displayName, email, identityType),
                Email = FirstNotEmpty(email, "-"),
                IsGroup = identityType is "group" or "siteGroup" or "application",
                IsExternal = GetString(identity, "userType").Equals("Guest", StringComparison.OrdinalIgnoreCase)
                    || email.Contains("#EXT#", StringComparison.OrdinalIgnoreCase)
            });
        }
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property))
            return string.Empty;

        return property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : property.ToString();
    }

    private static string FirstNotEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string NormalizeLoginName(string loginName)
    {
        int separator = loginName.LastIndexOf('|');
        return separator >= 0 ? loginName[(separator + 1)..] : loginName;
    }

    public async Task<string> CreateFolderAsync(string folderName)
    {
        string safeFolderName = SanitizeFolderName(folderName);
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();

        string currentFolderName = safeFolderName;
        int counter = 1;
        bool success = false;
        while (!success)
        {
            string json = $$"""
            {
                "name": "{{currentFolderName}}",
                "folder": {},
                "@microsoft.graph.conflictBehavior": "fail"
            }
            """;
            using StringContent content = new(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync($"https://graph.microsoft.com/v1.0/drives/{library.Id}/root/children", content);
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                currentFolderName = $"{safeFolderName} ({counter})";
                counter++;
            }
            else
            {
                response.EnsureSuccessStatusCode();
                success = true;
            }
        }
        return currentFolderName;
    }

    public async Task<List<DriveItem>> GetFoldersAsync()
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();
        string? nextUrl = $"https://graph.microsoft.com/v1.0/drives/{library.Id}/root/children?$top=200";
        var folders = new List<DriveItem>();

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            HttpResponseMessage response = await client.GetAsync(nextUrl);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            DrivesResponse? page = JsonSerializer.Deserialize<DrivesResponse>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (page is null) break;

            folders.AddRange(page.Value.Where(item => item.Folder is not null));
            nextUrl = page.NextLink;
        }

        return folders;
    }

    public async Task<IReadOnlyList<RecentSharePointActivity>> GetRecentActivitiesAsync(
        string? itemId,
        CancellationToken cancellationToken = default)
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();
        string? nextUrl = string.IsNullOrWhiteSpace(itemId)
            ? $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(library.Id)}/activities"
            : $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(library.Id)}/items/{Uri.EscapeDataString(itemId)}/activities";
        var result = new List<RecentSharePointActivity>();

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            using HttpResponseMessage response = await client.GetAsync(nextUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Lettura delle attività recenti SharePoint non riuscita ({response.StatusCode}): {error}");
            }

            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("value", out JsonElement activities))
            {
                foreach (JsonElement activity in activities.EnumerateArray())
                    result.Add(ParseRecentActivity(activity));
            }
            nextUrl = document.RootElement.TryGetProperty("@odata.nextLink", out JsonElement next)
                ? next.GetString()
                : null;
        }

        await EnrichRecentActivitiesAsync(client, library.Id, result, cancellationToken);
        return result.OrderByDescending(entry => entry.Timestamp).ToArray();
    }

    private static async Task EnrichRecentActivitiesAsync(
        HttpClient client,
        string driveId,
        List<RecentSharePointActivity> activities,
        CancellationToken cancellationToken)
    {
        string[] itemIds = activities
            .Where(entry => !string.IsNullOrWhiteSpace(entry.ItemId))
            .Select(entry => entry.ItemId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (itemIds.Length == 0) return;

        var metadata = new System.Collections.Concurrent.ConcurrentDictionary<string, (string Name, string Type, string Path)>(
            StringComparer.OrdinalIgnoreCase);
        using var limiter = new SemaphoreSlim(6, 6);
        await Task.WhenAll(itemIds.Select(async itemId =>
        {
            await limiter.WaitAsync(cancellationToken);
            try
            {
                using HttpResponseMessage response = await client.GetAsync(
                    $"https://graph.microsoft.com/v1.0/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(itemId)}?$select=id,name,webUrl,parentReference,folder,file",
                    cancellationToken);
                if (!response.IsSuccessStatusCode) return;
                using JsonDocument document = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(cancellationToken));
                JsonElement item = document.RootElement;
                string name = GetString(item, "name");
                string type = item.TryGetProperty("folder", out _) ? "Cartella"
                    : item.TryGetProperty("file", out _) ? "File" : "Elemento";
                string path = item.TryGetProperty("parentReference", out JsonElement parent)
                    ? GetString(parent, "path")
                    : GetString(item, "webUrl");
                if (!string.IsNullOrWhiteSpace(name)) metadata[itemId] = (name, type, path);
            }
            finally
            {
                limiter.Release();
            }
        }));

        for (int index = 0; index < activities.Count; index++)
        {
            RecentSharePointActivity entry = activities[index];
            if (!metadata.TryGetValue(entry.ItemId, out var item)) continue;
            activities[index] = entry with
            {
                ItemName = item.Name,
                ItemType = item.Type,
                Path = entry.Path == "-" && !string.IsNullOrWhiteSpace(item.Path) ? item.Path : entry.Path
            };
        }
    }

    private static RecentSharePointActivity ParseRecentActivity(JsonElement activity)
    {
        string timestampText = GetString(activity, "activityDateTime");
        if (string.IsNullOrWhiteSpace(timestampText)
            && activity.TryGetProperty("times", out JsonElement times))
        {
            timestampText = GetString(times, "recordedDateTime");
            if (string.IsNullOrWhiteSpace(timestampText))
                timestampText = GetString(times, "observedDateTime");
        }
        DateTimeOffset.TryParse(timestampText, out DateTimeOffset timestamp);
        JsonElement actor = activity.TryGetProperty("actor", out JsonElement actorValue)
            ? actorValue
            : default;
        string user = GetActorName(actor);
        JsonElement driveItem = activity.TryGetProperty("driveItem", out JsonElement itemValue)
            ? itemValue
            : default;
        string itemName = GetString(driveItem, "name");
        if (string.IsNullOrWhiteSpace(itemName)
            && activity.TryGetProperty("listItem", out JsonElement listItem))
        {
            itemName = GetString(listItem, "name");
            if (string.IsNullOrWhiteSpace(itemName)
                && listItem.TryGetProperty("fields", out JsonElement fields))
                itemName = GetString(fields, "FileLeafRef");
        }
        string itemType = driveItem.ValueKind == JsonValueKind.Object && driveItem.TryGetProperty("folder", out _)
            ? "Cartella"
            : driveItem.ValueKind == JsonValueKind.Object && driveItem.TryGetProperty("file", out _)
                ? "File"
                : "Elemento";
        string path = driveItem.ValueKind == JsonValueKind.Object
                      && driveItem.TryGetProperty("parentReference", out JsonElement parent)
            ? GetString(parent, "path")
            : string.Empty;
        if (string.IsNullOrWhiteSpace(path)) path = GetString(driveItem, "webUrl");
        JsonElement actions = activity.TryGetProperty("action", out JsonElement actionValue)
            && actionValue.ValueKind == JsonValueKind.Object
                ? actionValue
                : activity;
        (string operation, string details) = DescribeActivity(actions, ref itemName, ref itemType);
        string itemId = GetString(driveItem, "id");
        return new RecentSharePointActivity(
            timestamp,
            string.IsNullOrWhiteSpace(user) ? "-" : user,
            operation,
            string.IsNullOrWhiteSpace(itemName) ? "-" : itemName,
            itemType,
            details,
            string.IsNullOrWhiteSpace(path) ? "-" : path,
            itemId);
    }

    private static string GetActorName(JsonElement actor)
    {
        if (actor.ValueKind != JsonValueKind.Object) return string.Empty;
        foreach (string kind in new[] { "user", "application", "device" })
            if (actor.TryGetProperty(kind, out JsonElement identity))
            {
                string displayName = GetString(identity, "displayName");
                if (!string.IsNullOrWhiteSpace(displayName)) return displayName;
            }
        return string.Empty;
    }

    private static (string Operation, string Details) DescribeActivity(
        JsonElement activity,
        ref string itemName,
        ref string itemType)
    {
        if (activity.TryGetProperty("rename", out JsonElement rename))
        {
            string oldName = GetString(rename, "oldName");
            string newName = GetString(rename, "newName");
            if (!string.IsNullOrWhiteSpace(newName)) itemName = newName;
            else if (!string.IsNullOrWhiteSpace(oldName)) itemName = oldName;
            return ("Rinomina", $"Da '{oldName}' a '{newName}'");
        }
        if (activity.TryGetProperty("move", out JsonElement move))
        {
            string from = GetString(move, "from");
            string to = GetString(move, "to");
            string pathName = ExtractNameFromActivityPath(to);
            if (string.IsNullOrWhiteSpace(pathName)) pathName = ExtractNameFromActivityPath(from);
            if (!string.IsNullOrWhiteSpace(pathName)) itemName = pathName;
            return ("Spostamento", $"Da '{from}' a '{to}'");
        }
        if (activity.TryGetProperty("delete", out JsonElement delete))
        {
            string deletedName = GetString(delete, "name");
            if (!string.IsNullOrWhiteSpace(deletedName)) itemName = deletedName;
            string deletedType = GetString(delete, "objectType");
            if (!string.IsNullOrWhiteSpace(deletedType))
                itemType = deletedType.Equals("Folder", StringComparison.OrdinalIgnoreCase) ? "Cartella" : "File";
            return ("Eliminazione", "Elemento spostato nel cestino");
        }
        if (activity.TryGetProperty("create", out _)) return ("Creazione", string.Empty);
        if (activity.TryGetProperty("edit", out _)) return ("Modifica", string.Empty);
        if (activity.TryGetProperty("restore", out _)) return ("Ripristino", string.Empty);
        if (activity.TryGetProperty("share", out _)) return ("Condivisione", string.Empty);
        if (activity.TryGetProperty("access", out _)) return ("Accesso", string.Empty);
        if (activity.TryGetProperty("comment", out _)) return ("Commento", string.Empty);
        if (activity.TryGetProperty("mention", out _)) return ("Menzione", string.Empty);
        if (activity.TryGetProperty("version", out JsonElement version))
            return ("Nuova versione", GetString(version, "newVersion"));
        return ("Attività", string.Empty);
    }

    private static string ExtractNameFromActivityPath(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        string normalized = value.Trim().Trim('\'', '"').Replace('\\', '/').TrimEnd('/');
        int separator = normalized.LastIndexOf('/');
        return separator >= 0 ? normalized[(separator + 1)..].Trim('\'', '"') : string.Empty;
    }

    public async Task<string> RenameFolderAsync(string itemId, string folderName)
    {
        string safeFolderName = SanitizeFolderName(folderName);
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"https://graph.microsoft.com/v1.0/drives/{library.Id}/items/{itemId}")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { name = safeFolderName }),
                Encoding.UTF8,
                "application/json")
        };
        HttpResponseMessage response = await client.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            throw new InvalidOperationException($"Esiste già una condivisione denominata '{safeFolderName}'. Scegli un nome diverso.");
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Rinomina della condivisione non riuscita ({response.StatusCode}): {error}");
        }

        string json = await response.Content.ReadAsStringAsync();
        DriveItem? renamed = JsonSerializer.Deserialize<DriveItem>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return string.IsNullOrWhiteSpace(renamed?.Name) ? safeFolderName : renamed.Name;
    }

    public async Task<FolderMetadata> GetFolderMetadataAsync(string itemId)
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        Dictionary<string, string> columns = await GetMetadataColumnNamesAsync(library.Id);
        using HttpClient client = CreateHttpClient();
        HttpResponseMessage response = await client.GetAsync(
            $"https://graph.microsoft.com/v1.0/drives/{library.Id}/items/{itemId}/listItem/fields");
        response.EnsureSuccessStatusCode();

        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement fields = document.RootElement;
        return new FolderMetadata(
            GetString(fields, columns["Richiesto da"]),
            GetString(fields, columns["Note"]),
            DateTimeOffset.TryParse(GetString(fields, columns["scadenza"]), out DateTimeOffset expiration)
                ? expiration
                : null);
    }

    public async Task UpdateFolderMetadataAsync(string itemId, string requestedBy, string notes, DateTimeOffset? expirationDate)
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        Dictionary<string, string> columns = await GetMetadataColumnNamesAsync(library.Id);
        using HttpClient client = CreateHttpClient();
        string url = $"https://graph.microsoft.com/v1.0/drives/{library.Id}/items/{itemId}/listItem/fields";
        var values = new Dictionary<string, object?>
        {
            [columns["Richiesto da"]] = requestedBy,
            [columns["Note"]] = notes,
            [columns["scadenza"]] = expirationDate?.ToString("O")
        };
        using StringContent content = new(JsonSerializer.Serialize(values), Encoding.UTF8, "application/json");
        using HttpRequestMessage request = new(HttpMethod.Patch, url) { Content = content };
        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdatePermissionRoleAsync(string itemId, string permissionId, string role)
    {
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();
        using HttpClient client = CreateHttpClient();
        string url = $"https://graph.microsoft.com/v1.0/drives/{library.Id}/items/{itemId}/permissions/{permissionId}";
        using StringContent content = new(
            JsonSerializer.Serialize(new { roles = new[] { role } }),
            Encoding.UTF8,
            "application/json");
        using HttpRequestMessage request = new(HttpMethod.Patch, url) { Content = content };
        HttpResponseMessage response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Errore SharePoint ({response.StatusCode}): {error}");
        }
    }

    public async Task<PracticeClosureResult> ClosePracticeAsync(string itemId, string internalDomain)
    {
        List<PermissionEntry> permissions = await GetPermissionsAsync(itemId);
        var revokedUsers = new List<string>();
        var errors = new List<string>();

        foreach (IGrouping<string, PermissionEntry> permissionGroup in permissions
                     .Where(permission => IsClosableExternalPermission(permission, internalDomain))
                     .GroupBy(permission => permission.Id))
        {
            try
            {
                await RemovePermissionAsync(itemId, permissionGroup.Key);
                revokedUsers.AddRange(permissionGroup.Select(permission => permission.Email));
            }
            catch (Exception ex)
            {
                errors.Add($"{string.Join(", ", permissionGroup.Select(permission => permission.Email))}: {ex.Message}");
            }
        }

        return new PracticeClosureResult(revokedUsers.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), errors);
    }

    public async Task<bool> HasClosablePermissionsAsync(string itemId, string internalDomain)
    {
        List<PermissionEntry> permissions = await GetPermissionsAsync(itemId);
        return permissions.Any(permission => IsClosableExternalPermission(permission, internalDomain));
    }

    private static bool IsClosableExternalPermission(PermissionEntry permission, string internalDomain)
    {
        if (permission.IsGroup
            || permission.Role.Equals("owner", StringComparison.OrdinalIgnoreCase)
            || !System.Net.Mail.MailAddress.TryCreate(permission.Email, out System.Net.Mail.MailAddress? address))
            return false;

        return permission.IsExternal
            || !address.Host.Equals(internalDomain, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<string, string>> GetMetadataColumnNamesAsync(string driveId)
    {
        if (_metadataColumnNames is not null) return _metadataColumnNames;

        await _metadataColumnsLock.WaitAsync();
        try
        {
            if (_metadataColumnNames is not null) return _metadataColumnNames;

            using HttpClient client = CreateHttpClient();
            HttpResponseMessage response = await client.GetAsync(
                $"https://graph.microsoft.com/v1.0/drives/{driveId}/list/columns?$select=name,displayName");
            response.EnsureSuccessStatusCode();
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            string[] requiredColumns = { "Richiesto da", "Note", "scadenza" };
            var columns = document.RootElement.GetProperty("value").EnumerateArray()
                .Where(column => requiredColumns.Contains(GetString(column, "displayName"), StringComparer.OrdinalIgnoreCase))
                .ToDictionary(column => GetString(column, "displayName"), column => GetString(column, "name"), StringComparer.OrdinalIgnoreCase);

            foreach (string requiredColumn in requiredColumns)
                if (!columns.ContainsKey(requiredColumn))
                    throw new InvalidOperationException($"La colonna SharePoint '{requiredColumn}' non è stata trovata nella document library.");

            _metadataColumnNames = columns;
            return columns;
        }
        finally
        {
            _metadataColumnsLock.Release();
        }
    }

    private string SanitizeFolderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "Nuova-Cartella";
        char[] invalidChars = { '"', '*', ':', '<', '>', '?', '/', '\\', '|', '#', '%' };
        string sanitized = name;
        foreach (char c in invalidChars) sanitized = sanitized.Replace(c, '-');
        return sanitized.Trim();
    }

    public async Task EnsureComplianceListsAsync()
    {
        string siteId = await GetSiteIdAsync();
        _acceptanceListId = await EnsureHiddenListAsync(siteId, "PresaVisioneDataRoom", new[]
        {
            TextColumn("UtenteId"),
            TextColumn("NomeUtente"),
            TextColumn("Email"),
            TextColumn("VersioneDisclaimer"),
            DateTimeColumn("DataPresaVisione"),
            TextColumn("VersioneApplicazione"),
            TextColumn("Esito"),
            TextColumn("TestoHash")
        });
        _activityListId = await EnsureHiddenListAsync(siteId, "DataRoomActivityLog", new[]
        {
            DateTimeColumn("DataEvento"),
            TextColumn("UtenteId"),
            TextColumn("NomeUtente"),
            TextColumn("Email"),
            TextColumn("Operazione"),
            TextColumn("PraticaId"),
            TextColumn("PraticaNome"),
            TextColumn("Esito"),
            TextColumn("Destinatario"),
            TextColumn("VersioneApplicazione"),
            TextColumn("Dettagli", allowMultipleLines: true)
        });
    }

    public async Task<bool> HasDisclaimerAcceptanceAsync(string userId, string disclaimerVersion)
    {
        await EnsureComplianceListsReadyAsync();
        using HttpClient client = CreateHttpClient();
        string? nextUrl = $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(_siteId!)}/lists/{_acceptanceListId}/items?$expand=fields&$top=200";

        while (!string.IsNullOrWhiteSpace(nextUrl))
        {
            HttpResponseMessage response = await client.GetAsync(nextUrl);
            response.EnsureSuccessStatusCode();
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            foreach (JsonElement item in document.RootElement.GetProperty("value").EnumerateArray())
            {
                if (!item.TryGetProperty("fields", out JsonElement fields)) continue;
                if (GetString(fields, "UtenteId").Equals(userId, StringComparison.OrdinalIgnoreCase)
                    && GetString(fields, "VersioneDisclaimer").Equals(disclaimerVersion, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            nextUrl = document.RootElement.TryGetProperty("@odata.nextLink", out JsonElement next)
                ? next.GetString()
                : null;
        }
        return false;
    }

    public async Task RecordDisclaimerAcceptanceAsync(
        AuthenticationService.CurrentUserProfile user,
        string disclaimerVersion,
        string disclaimerHash)
    {
        await EnsureComplianceListsReadyAsync();
        await CreateListItemAsync(_acceptanceListId!, new Dictionary<string, object?>
        {
            ["Title"] = $"{user.Email} - {disclaimerVersion}",
            ["UtenteId"] = user.Id,
            ["NomeUtente"] = user.DisplayName,
            ["Email"] = user.Email,
            ["VersioneDisclaimer"] = disclaimerVersion,
            ["DataPresaVisione"] = DateTimeOffset.UtcNow.ToString("O"),
            ["VersioneApplicazione"] = Application.ProductVersion,
            ["Esito"] = "Accettato",
            ["TestoHash"] = disclaimerHash
        });
    }

    public async Task RecordActivityAsync(AuthenticationService.CurrentUserProfile user, string message)
    {
        await RecordActivityAsync(
            user,
            "Diagnostica applicazione",
            message.Contains("[ERRORE]", StringComparison.OrdinalIgnoreCase) ? "Errore" : "Successo",
            null,
            null,
            null,
            message);
    }

    public async Task RecordActivityAsync(
        AuthenticationService.CurrentUserProfile user,
        string operation,
        string outcome,
        string? practiceId,
        string? practiceName,
        string? recipient,
        string details)
    {
        await EnsureComplianceListsReadyAsync();
        await CreateListItemAsync(_activityListId!, new Dictionary<string, object?>
        {
            ["Title"] = Guid.NewGuid().ToString("N"),
            ["DataEvento"] = DateTimeOffset.UtcNow.ToString("O"),
            ["UtenteId"] = user.Id,
            ["NomeUtente"] = user.DisplayName,
            ["Email"] = user.Email,
            ["Operazione"] = operation,
            ["PraticaId"] = practiceId,
            ["PraticaNome"] = practiceName,
            ["Esito"] = outcome,
            ["Destinatario"] = recipient,
            ["VersioneApplicazione"] = Application.ProductVersion,
            ["Dettagli"] = details.Length <= 4000 ? details : details[..4000]
        });
    }

    public async Task<IReadOnlyList<AuditActivity>> GetAuditActivitiesAsync()
    {
        await EnsureComplianceListsReadyAsync();
        IReadOnlyList<JsonElement> fields = await GetListItemFieldsAsync(_activityListId!);
        return fields.Select(item => new AuditActivity(
                ParseDate(GetString(item, "DataEvento")),
                GetString(item, "NomeUtente"),
                GetString(item, "Email"),
                GetString(item, "Operazione"),
                GetString(item, "PraticaNome"),
                GetString(item, "Esito"),
                GetString(item, "Destinatario"),
                GetString(item, "Dettagli")))
            .OrderByDescending(item => item.Timestamp)
            .ToList();
    }

    public async Task<IReadOnlyList<DisclaimerAcceptance>> GetDisclaimerAcceptancesAsync()
    {
        await EnsureComplianceListsReadyAsync();
        IReadOnlyList<JsonElement> fields = await GetListItemFieldsAsync(_acceptanceListId!);
        return fields.Select(item => new DisclaimerAcceptance(
                ParseDate(GetString(item, "DataPresaVisione")),
                GetString(item, "NomeUtente"),
                GetString(item, "Email"),
                GetString(item, "VersioneDisclaimer"),
                GetString(item, "VersioneApplicazione"),
                GetString(item, "Esito"),
                GetString(item, "TestoHash")))
            .OrderByDescending(item => item.Timestamp)
            .ToList();
    }

    private async Task<IReadOnlyList<JsonElement>> GetListItemFieldsAsync(string listId)
    {
        string siteId = await GetSiteIdAsync();
        using HttpClient client = CreateHttpClient();
        string? nextUrl = $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/lists/{listId}/items?$expand=fields&$top=200";
        var result = new List<JsonElement>();

        while (!string.IsNullOrWhiteSpace(nextUrl) && result.Count < 2000)
        {
            HttpResponseMessage response = await client.GetAsync(nextUrl);
            response.EnsureSuccessStatusCode();
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            foreach (JsonElement item in document.RootElement.GetProperty("value").EnumerateArray())
            {
                if (item.TryGetProperty("fields", out JsonElement itemFields))
                    result.Add(itemFields.Clone());
                if (result.Count >= 2000) break;
            }
            nextUrl = document.RootElement.TryGetProperty("@odata.nextLink", out JsonElement next)
                ? next.GetString()
                : null;
        }
        return result;
    }

    private static DateTimeOffset ParseDate(string value) =>
        DateTimeOffset.TryParse(value, out DateTimeOffset parsed) ? parsed : DateTimeOffset.MinValue;

    private async Task EnsureComplianceListsReadyAsync()
    {
        if (_acceptanceListId is null || _activityListId is null)
            await EnsureComplianceListsAsync();
    }

    private async Task<string> GetSiteIdAsync()
    {
        if (!string.IsNullOrWhiteSpace(_siteId)) return _siteId;
        Uri siteUri = new(_options.SiteUrl);
        using HttpClient client = CreateHttpClient();
        HttpResponseMessage response = await client.GetAsync(
            $"https://graph.microsoft.com/v1.0/sites/{siteUri.Host}:{siteUri.AbsolutePath}?$select=id");
        response.EnsureSuccessStatusCode();
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _siteId = document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Microsoft Graph non ha restituito l'ID del sito SharePoint.");
        return _siteId;
    }

    private async Task<string> EnsureHiddenListAsync(
        string siteId,
        string displayName,
        IReadOnlyCollection<Dictionary<string, object>> columns)
    {
        using HttpClient client = CreateHttpClient();
        string listsUrl = $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/lists?$select=id,displayName,list";
        HttpResponseMessage getResponse = await client.GetAsync(listsUrl);
        getResponse.EnsureSuccessStatusCode();
        using JsonDocument lists = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        JsonElement existing = lists.RootElement.GetProperty("value").EnumerateArray()
            .FirstOrDefault(item => GetString(item, "displayName").Equals(displayName, StringComparison.OrdinalIgnoreCase));

        if (existing.ValueKind != JsonValueKind.Undefined)
        {
            string existingId = GetString(existing, "id");
            await EnsureListColumnsAsync(client, siteId, existingId, columns);
            return existingId;
        }

        var body = new
        {
            displayName,
            list = new { template = "genericList", hidden = true }
        };
        using StringContent content = new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        HttpResponseMessage createResponse = await client.PostAsync(
            $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/lists",
            content);
        if (!createResponse.IsSuccessStatusCode)
        {
            string error = await createResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Creazione della lista nascosta '{displayName}' non riuscita ({createResponse.StatusCode}): {error}");
        }
        using JsonDocument created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        string createdId = created.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException($"ID della lista '{displayName}' non restituito da Microsoft Graph.");
        await EnsureListColumnsAsync(client, siteId, createdId, columns);
        return createdId;
    }

    private static async Task EnsureListColumnsAsync(
        HttpClient client,
        string siteId,
        string listId,
        IReadOnlyCollection<Dictionary<string, object>> requiredColumns)
    {
        string columnsUrl = $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/lists/{listId}/columns";
        HttpResponseMessage getResponse = await client.GetAsync($"{columnsUrl}?$select=name,displayName");
        getResponse.EnsureSuccessStatusCode();
        using JsonDocument existingColumns = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        HashSet<string> existingNames = existingColumns.RootElement.GetProperty("value").EnumerateArray()
            .Select(column => GetString(column, "name"))
            .Concat(existingColumns.RootElement.GetProperty("value").EnumerateArray()
                .Select(column => GetString(column, "displayName")))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (Dictionary<string, object> column in requiredColumns)
        {
            string name = column["name"].ToString() ?? string.Empty;
            if (existingNames.Contains(name)) continue;

            using StringContent content = new(JsonSerializer.Serialize(column), Encoding.UTF8, "application/json");
            HttpResponseMessage createResponse = await client.PostAsync(columnsUrl, content);
            if (!createResponse.IsSuccessStatusCode)
            {
                string error = await createResponse.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Creazione della colonna '{name}' non riuscita ({createResponse.StatusCode}): {error}");
            }
            existingNames.Add(name);
        }
    }

    private async Task CreateListItemAsync(string listId, Dictionary<string, object?> fields)
    {
        string siteId = await GetSiteIdAsync();
        using HttpClient client = CreateHttpClient();
        using StringContent content = new(
            JsonSerializer.Serialize(new { fields }),
            Encoding.UTF8,
            "application/json");
        HttpResponseMessage response = await client.PostAsync(
            $"https://graph.microsoft.com/v1.0/sites/{Uri.EscapeDataString(siteId)}/lists/{listId}/items",
            content);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Registrazione SharePoint non riuscita ({response.StatusCode}): {error}");
        }
    }

    private static Dictionary<string, object> TextColumn(string name, bool allowMultipleLines = false) => new()
    {
        ["name"] = name,
        ["text"] = new { allowMultipleLines }
    };

    private static Dictionary<string, object> DateTimeColumn(string name) => new()
    {
        ["name"] = name,
        ["dateTime"] = new { format = "dateTime" }
    };
}
