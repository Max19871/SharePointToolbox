using Microsoft.Extensions.Options;
using SharePointToolbox.Configuration;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharePointToolbox.Microsoft365;

public sealed class PressReviewService : GraphClient
{
    private const int UploadChunkSize = 10 * 1024 * 1024;
    private readonly PressReviewOptions _options;
    private readonly MailOptions _mail;
    private readonly ThemeOptions _theme;
    private string? _siteId;
    private DriveInfo? _library;
    public bool IsVerified => _library is not null;

    public sealed record PublicationResult(
        string FolderName,
        string FolderUrl,
        IReadOnlyList<string> UploadedFiles);
    public sealed record ReviewEntry(DateTime Date, int FileCount);
    public sealed record ReviewFile(string Id, string Name, long Size);

    private sealed class CollectionResponse<T>
    {
        public List<T> Value { get; set; } = new();
    }

    private sealed class DriveInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string WebUrl { get; set; } = string.Empty;
    }

    private sealed class ItemInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string WebUrl { get; set; } = string.Empty;
        public FolderInfo? Folder { get; set; }
        public long? Size { get; set; }
    }

    private sealed class FolderInfo
    {
        public int ChildCount { get; set; }
    }

    private sealed class UploadSessionInfo
    {
        public string UploadUrl { get; set; } = string.Empty;
    }

    public PressReviewService(
        AuthenticationService authenticationService,
        IOptions<PressReviewOptions> options,
        IOptions<MailOptions> mail,
        IOptions<ThemeOptions> theme)
        : base(authenticationService)
    {
        _options = options.Value;
        _mail = mail.Value;
        _theme = theme.Value;
    }

    public async Task VerifyAsync()
    {
        _ = await GetLibraryAsync();
    }

    public async Task<IReadOnlySet<string>> GetExistingFileNamesAsync(DateTime date)
    {
        DriveInfo drive = await GetLibraryAsync();
        ItemInfo? day = await FindFolderPathAsync(drive.Id, DateParts(date), create: false);
        if (day is null) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        CollectionResponse<ItemInfo> children = await GetAsync<CollectionResponse<ItemInfo>>(
            $"https://graph.microsoft.com/v1.0/drives/{Escape(drive.Id)}/items/{Escape(day.Id)}/children?$select=name,folder");
        return children.Value
            .Where(item => item.Folder is null)
            .Select(item => item.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<ReviewEntry>> GetArchiveAsync()
    {
        DriveInfo drive = await GetLibraryAsync();
        CollectionResponse<ItemInfo> years = await GetChildrenAsync(drive.Id, null);
        var entries = new List<ReviewEntry>();
        foreach (ItemInfo year in years.Value.Where(item =>
                     item.Folder is not null && int.TryParse(item.Name, out _)))
        {
            CollectionResponse<ItemInfo> months = await GetChildrenAsync(drive.Id, year.Id);
            foreach (ItemInfo month in months.Value.Where(item => item.Folder is not null))
            {
                CollectionResponse<ItemInfo> days = await GetChildrenAsync(drive.Id, month.Id);
                foreach (ItemInfo day in days.Value.Where(item => item.Folder is not null))
                {
                    if (DateTime.TryParseExact(
                            day.Name,
                            "dd-MM-yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime date))
                        entries.Add(new ReviewEntry(date, day.Folder?.ChildCount ?? 0));
                }
            }
        }
        return entries.OrderByDescending(entry => entry.Date).ToArray();
    }

    public async Task<IReadOnlyList<ReviewFile>> GetReviewFilesAsync(DateTime date)
    {
        DriveInfo drive = await GetLibraryAsync();
        ItemInfo? day = await FindFolderPathAsync(drive.Id, DateParts(date), create: false);
        if (day is null) return Array.Empty<ReviewFile>();
        CollectionResponse<ItemInfo> children = await GetChildrenAsync(drive.Id, day.Id);
        return children.Value
            .Where(item => item.Folder is null)
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(item => new ReviewFile(item.Id, item.Name, item.Size ?? 0))
            .ToArray();
    }

    public async Task DownloadFileAsync(
        string itemId,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        DriveInfo drive = await GetLibraryAsync();
        using HttpClient client = CreateHttpClient();
        string url =
            $"https://graph.microsoft.com/v1.0/drives/{Escape(drive.Id)}"
            + $"/items/{Escape(itemId)}/content";
        using HttpResponseMessage response = await client.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using Stream source = await response.Content.ReadAsStreamAsync(
            cancellationToken);
        await using var destination = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);
        await source.CopyToAsync(destination, cancellationToken);
    }

    public async Task<PublicationResult> PublishAsync(
        DateTime reviewDate,
        IReadOnlyList<string> filePaths,
        IReadOnlyList<string>? fileIdsToDelete = null,
        bool sendNotification = true,
        IProgress<(int Percentage, string Message)>? progress = null)
    {
        fileIdsToDelete ??= Array.Empty<string>();
        if (filePaths.Count == 0 && fileIdsToDelete.Count == 0)
            throw new InvalidOperationException("Non risultano modifiche da pubblicare.");

        DriveInfo drive = await GetLibraryAsync();
        progress?.Report((4, "Preparazione dell’archivio della rassegna…"));
        string[] parts = DateParts(reviewDate);
        ItemInfo day = await FindFolderPathAsync(drive.Id, parts, create: true)
            ?? throw new InvalidOperationException("Non è stato possibile creare la cartella della rassegna.");

        string sharingUrl = await CreateOrganizationReadLinkAsync(drive.Id, day.Id);

        for (int index = 0; index < fileIdsToDelete.Count; index++)
        {
            progress?.Report((6, "Rimozione dei documenti selezionati…"));
            await DeleteItemAsync(drive.Id, fileIdsToDelete[index]);
        }

        var uploaded = new List<string>();
        for (int index = 0; index < filePaths.Count; index++)
        {
            string filePath = filePaths[index];
            int start = 8 + (int)Math.Round(78D * index / filePaths.Count);
            int span = Math.Max(1, (int)Math.Round(78D / filePaths.Count));
            progress?.Report((start, $"Caricamento di {Path.GetFileName(filePath)}…"));
            await UploadFileAsync(
                drive.Id,
                day.Id,
                filePath,
                value =>
                {
                    int percentage = start + (int)Math.Round(span * value / 100D);
                    progress?.Report((Math.Min(86, percentage), $"Caricamento di {Path.GetFileName(filePath)}…"));
                });
            uploaded.Add(Path.GetFileName(filePath));
        }

        CollectionResponse<ItemInfo> dayContents = await GetAsync<CollectionResponse<ItemInfo>>(
            $"https://graph.microsoft.com/v1.0/drives/{Escape(drive.Id)}/items/{Escape(day.Id)}/children?$select=name,folder");
        string[] availableFiles = dayContents.Value
            .Where(item => item.Folder is null)
            .Select(item => item.Name)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        if (sendNotification)
        {
            progress?.Report((90, "Invio della notifica…"));
            await SendNotificationAsync(reviewDate, sharingUrl, availableFiles);
        }
        else
        {
            progress?.Report((96, "Salvataggio delle modifiche…"));
        }
        progress?.Report((100, "Rassegna stampa pubblicata."));
        return new PublicationResult(day.Name, sharingUrl, uploaded);
    }

    public async Task ResendNotificationAsync(
        DateTime reviewDate,
        string recipient,
        IProgress<(int Percentage, string Message)>? progress = null)
    {
        DriveInfo drive = await GetLibraryAsync();
        progress?.Report((20, "Preparazione del collegamento…"));
        ItemInfo? day = await FindFolderPathAsync(
            drive.Id,
            DateParts(reviewDate),
            create: false);
        if (day is null)
            throw new InvalidOperationException("La rassegna selezionata non è più disponibile.");

        CollectionResponse<ItemInfo> dayContents = await GetAsync<CollectionResponse<ItemInfo>>(
            $"https://graph.microsoft.com/v1.0/drives/{Escape(drive.Id)}/items/{Escape(day.Id)}/children?$select=name,folder");
        string[] availableFiles = dayContents.Value
            .Where(item => item.Folder is null)
            .Select(item => item.Name)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        if (availableFiles.Length == 0)
            throw new InvalidOperationException("La rassegna selezionata non contiene documenti.");

        string sharingUrl = await CreateOrganizationReadLinkAsync(drive.Id, day.Id);
        progress?.Report((70, "Reinvio del collegamento…"));
        await SendNotificationAsync(reviewDate, sharingUrl, availableFiles, recipient);
        progress?.Report((100, "Collegamento reinviato."));
    }

    private async Task DeleteItemAsync(string driveId, string itemId)
    {
        using HttpClient client = CreateHttpClient();
        HttpResponseMessage response = await client.DeleteAsync(
            $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/items/{Escape(itemId)}");
        await EnsureSuccessAsync(response, "Rimozione del documento dalla rassegna");
    }

    private async Task<DriveInfo> GetLibraryAsync()
    {
        if (_library is not null) return _library;
        string siteId = await GetSiteIdAsync();
        CollectionResponse<DriveInfo> drives = await GetAsync<CollectionResponse<DriveInfo>>(
            $"https://graph.microsoft.com/v1.0/sites/{Escape(siteId)}/drives");
        _library = drives.Value.FirstOrDefault(drive =>
                string.Equals(drive.Name, _options.LibraryName, StringComparison.OrdinalIgnoreCase))
            ?? drives.Value.FirstOrDefault(drive =>
                LibraryUrlMatches(drive.WebUrl, _options.LibraryName));

        // Nei siti localizzati SharePoint può restituire, per esempio, il nome
        // visualizzato "Documenti" e conservare "Documenti condivisi" nell'URL.
        // Se il sito dedicato espone una sola raccolta, non è necessario
        // dipendere da nessuna delle due denominazioni.
        if (_library is null && drives.Value.Count == 1)
            _library = drives.Value[0];

        if (_library is null)
        {
            string available = drives.Value.Count == 0
                ? "nessuna raccolta disponibile"
                : string.Join(", ", drives.Value.Select(drive => drive.Name));
            throw new InvalidOperationException(
                $"La raccolta '{_options.LibraryName}' non è stata trovata nel sito Rassegna Stampa. Raccolte disponibili: {available}.");
        }
        return _library;
    }

    private static bool LibraryUrlMatches(string webUrl, string configuredName)
    {
        if (!Uri.TryCreate(webUrl, UriKind.Absolute, out Uri? uri)) return false;
        string lastSegment = Uri.UnescapeDataString(
            uri.AbsolutePath.TrimEnd('/').Split('/').LastOrDefault() ?? string.Empty);
        return string.Equals(lastSegment, configuredName, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> GetSiteIdAsync()
    {
        if (!string.IsNullOrWhiteSpace(_siteId)) return _siteId;
        if (!Uri.TryCreate(_options.SiteUrl, UriKind.Absolute, out Uri? siteUri))
            throw new InvalidOperationException("URL del sito Rassegna Stampa non valido.");
        string path = siteUri.AbsolutePath.TrimEnd('/');
        using HttpClient client = CreateHttpClient();
        HttpResponseMessage response = await client.GetAsync(
            $"https://graph.microsoft.com/v1.0/sites/{siteUri.Host}:{path}?$select=id");
        await EnsureSuccessAsync(response, "Connessione al sito Rassegna Stampa");
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _siteId = json.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Microsoft Graph non ha restituito l’ID del sito.");
        return _siteId;
    }

    private async Task<ItemInfo?> FindFolderPathAsync(
        string driveId,
        IReadOnlyList<string> parts,
        bool create)
    {
        ItemInfo? current = null;
        foreach (string part in parts)
        {
            string childrenUrl = current is null
                ? $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/root/children?$select=id,name,webUrl,folder"
                : $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/items/{Escape(current.Id)}/children?$select=id,name,webUrl,folder";
            CollectionResponse<ItemInfo> children =
                await GetAsync<CollectionResponse<ItemInfo>>(childrenUrl);
            ItemInfo? found = children.Value.FirstOrDefault(item =>
                item.Folder is not null &&
                string.Equals(item.Name, part, StringComparison.OrdinalIgnoreCase));
            if (found is null)
            {
                if (!create) return null;
                found = await CreateFolderAsync(driveId, current?.Id, part);
            }
            current = found;
        }
        return current;
    }

    private Task<CollectionResponse<ItemInfo>> GetChildrenAsync(
        string driveId,
        string? parentId)
    {
        string url = parentId is null
            ? $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/root/children?$select=id,name,webUrl,size,folder"
            : $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/items/{Escape(parentId)}/children?$select=id,name,webUrl,size,folder";
        return GetAsync<CollectionResponse<ItemInfo>>(url);
    }

    private async Task<ItemInfo> CreateFolderAsync(string driveId, string? parentId, string name)
    {
        string url = parentId is null
            ? $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/root/children"
            : $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/items/{Escape(parentId)}/children";
        var body = new Dictionary<string, object>
        {
            ["name"] = name,
            ["folder"] = new { },
            ["@microsoft.graph.conflictBehavior"] = "fail"
        };
        return await PostAsync<ItemInfo>(url, body);
    }

    private async Task<string> CreateOrganizationReadLinkAsync(string driveId, string itemId)
    {
        JsonElement result = await PostAsync<JsonElement>(
            $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/items/{Escape(itemId)}/createLink",
            new { type = "view", scope = "organization" });
        if (result.TryGetProperty("link", out JsonElement link)
            && link.TryGetProperty("webUrl", out JsonElement webUrl)
            && !string.IsNullOrWhiteSpace(webUrl.GetString()))
            return webUrl.GetString()!;
        throw new InvalidOperationException(
            "Microsoft Graph non ha restituito il collegamento della rassegna.");
    }

    private async Task UploadFileAsync(
        string driveId,
        string parentId,
        string filePath,
        Action<int> report)
    {
        string fileName = Path.GetFileName(filePath);
        string url =
            $"https://graph.microsoft.com/v1.0/drives/{Escape(driveId)}/items/{Escape(parentId)}:/{EscapePath(fileName)}:/createUploadSession";
        UploadSessionInfo session = await PostAsync<UploadSessionInfo>(
            url,
            new { item = new Dictionary<string, object> { ["@microsoft.graph.conflictBehavior"] = "replace" } });
        if (string.IsNullOrWhiteSpace(session.UploadUrl))
            throw new InvalidOperationException($"Sessione di caricamento non creata per '{fileName}'.");

        await using FileStream stream = File.OpenRead(filePath);
        long total = stream.Length;
        byte[] buffer = new byte[UploadChunkSize];
        long offset = 0;
        using var uploadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        while (offset < total)
        {
            int requested = (int)Math.Min(buffer.Length, total - offset);
            int read = await stream.ReadAsync(buffer.AsMemory(0, requested));
            if (read <= 0) break;
            using var content = new ByteArrayContent(buffer, 0, read);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Headers.ContentLength = read;
            content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + read - 1, total);
            HttpResponseMessage response = await uploadClient.PutAsync(session.UploadUrl, content);
            await EnsureSuccessAsync(response, $"Caricamento di '{fileName}'");
            offset += read;
            report(total == 0 ? 100 : (int)Math.Round(100D * offset / total));
        }
    }

    private async Task SendNotificationAsync(
        DateTime reviewDate,
        string folderUrl,
        IReadOnlyList<string> files,
        string? recipientOverride = null)
    {
        string recipient = string.IsNullOrWhiteSpace(recipientOverride)
            ? _options.NotificationRecipient.Trim()
            : recipientOverride.Trim();
        string sender = _options.SenderMailbox.Trim();
        if (recipient.Length == 0 || sender.Length == 0)
            throw new InvalidOperationException("Mittente o destinatario della Rassegna Stampa non configurato.");

        CultureInfo italian = CultureInfo.GetCultureInfo("it-IT");
        string readableDate = reviewDate.ToString("d MMMM yyyy", italian);
        string subject = _options.SubjectTemplate.Replace(
            "{DataRassegna}", readableDate, StringComparison.OrdinalIgnoreCase);
        string template = await File.ReadAllTextAsync(ResourcePath(_options.TemplatePath), Encoding.UTF8);
        string fileList = string.Join(
            string.Empty,
            files.Select(file => $"<li style=\"margin:3px 0;\">{Html(file)}</li>"));
        string documentsBlock = $"""
            <div style="margin:20px 0;padding:14px 16px;background:#f5f7fa;border-left:4px solid {_theme.InstitutionalAccent};font-size:13px;line-height:20px;color:#444444;">
              <strong>Documenti disponibili: {files.Count}</strong>
              <ul style="margin:8px 0 0;padding-left:20px;">{fileList}</ul>
            </div>
            """;
        var tokens = new Dictionary<string, string>
        {
            ["Subject"] = Html(subject),
            ["OfficeName"] = Html(_mail.OfficeTitle),
            ["ProductLabel"] = "RASSEGNA STAMPA",
            ["Introduction"] = "È disponibile la rassegna stampa del giorno:",
            ["Condivisione"] = Html(readableDate),
            ["CustomMessageBlock"] = documentsBlock,
            ["LinkCondivisione"] = Html(folderUrl),
            ["ButtonText"] = "APRI LA RASSEGNA STAMPA",
            ["AccessNotice"] = "Il collegamento è riservato agli utenti dell’organizzazione ed è disponibile in sola lettura.",
            ["ConfidentialityBlock"] = string.Empty,
            ["OfficeTitle"] = Html(_mail.OfficeTitle),
            ["OfficeAddress"] = Html(_mail.OfficeAddress),
            ["OfficePhone"] = Html(_mail.OfficePhone),
            ["SupportText"] = Html(_mail.SupportText),
            ["SupportAddress"] = Html(_mail.SupportAddress),
            ["DataRassegna"] = Html(readableDate),
            ["FolderUrl"] = Html(folderUrl),
            ["FileCount"] = files.Count.ToString(CultureInfo.InvariantCulture),
            ["FileList"] = fileList,
            ["PrimaryColor"] = _theme.Primary,
            ["PrimaryDarkColor"] = _theme.PrimaryDark,
            ["InstitutionalAccent"] = _theme.InstitutionalAccent
        };
        foreach ((string token, string value) in tokens)
            template = template.Replace($"{{{{{token}}}}}", value, StringComparison.Ordinal);

        object[] attachments =
        {
            InlineImage(ResourcePath(_options.LogoPath), "studio-logo", "logo-studio.png"),
            InlineImage(ResourcePath(_options.WatermarkPath), "building-watermark", "filigrana-palazzo.png")
        };
        var body = new
        {
            message = new
            {
                subject,
                body = new { contentType = "HTML", content = template },
                from = new { emailAddress = new { address = sender } },
                toRecipients = new[] { new { emailAddress = new { address = recipient } } },
                attachments
            },
            saveToSentItems = true
        };
        using HttpClient client = CreateHttpClient();
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(
            "https://graph.microsoft.com/v1.0/me/sendMail", content);
        await EnsureSuccessAsync(response, "Invio della rassegna stampa");
    }

    private static string[] DateParts(DateTime date) =>
        new[]
        {
            date.ToString("yyyy", CultureInfo.InvariantCulture),
            date.ToString("MM", CultureInfo.InvariantCulture) + " - " +
                CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.GetMonthName(date.Month)
                    .ToUpperInvariant()[..1] +
                CultureInfo.GetCultureInfo("it-IT").DateTimeFormat.GetMonthName(date.Month)[1..],
            date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
        };

    private async Task<T> GetAsync<T>(string url)
    {
        using HttpClient client = CreateHttpClient();
        HttpResponseMessage response = await client.GetAsync(url);
        await EnsureSuccessAsync(response, "Lettura dati Rassegna Stampa");
        return JsonSerializer.Deserialize<T>(
                   await response.Content.ReadAsStringAsync(),
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("Risposta Microsoft Graph non valida.");
    }

    private async Task<T> PostAsync<T>(string url, object body)
    {
        using HttpClient client = CreateHttpClient();
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync(url, content);
        await EnsureSuccessAsync(response, "Operazione Rassegna Stampa");
        return JsonSerializer.Deserialize<T>(
                   await response.Content.ReadAsStringAsync(),
                   new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new InvalidOperationException("Risposta Microsoft Graph non valida.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;
        string error = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"{operation} non riuscita ({response.StatusCode}): {error}");
    }

    private static Dictionary<string, object> InlineImage(string path, string contentId, string name)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Risorsa email non trovata: {path}", path);
        return new Dictionary<string, object>
        {
            ["@odata.type"] = "#microsoft.graph.fileAttachment",
            ["name"] = name,
            ["contentType"] = "image/png",
            ["contentBytes"] = Convert.ToBase64String(File.ReadAllBytes(path)),
            ["isInline"] = true,
            ["contentId"] = contentId
        };
    }

    private static string ResourcePath(string path) =>
        Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
    private static string Html(string? value) => System.Net.WebUtility.HtmlEncode(value ?? string.Empty);
    private static string Escape(string value) => Uri.EscapeDataString(value);
    private static string EscapePath(string value) => Uri.EscapeDataString(value).Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
}
