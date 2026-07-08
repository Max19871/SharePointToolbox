using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http.Headers;
using System.Text.Json;
using SharePointToolbox.Models;
using Microsoft.Extensions.Options;
using SharePointToolbox.Configuration;

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
            throw new Exception("Nessuna Document Library trovata.");

        DriveItem drive = response.Value[0];

        return new DocumentLibrary
        {
            Id = drive.Id,
            Name = drive.Name,
            WebUrl = drive.WebUrl
        };
    }

    /// <summary>
    /// Crea una cartella nella Document Library.
    /// </summary>
    public async Task CreateFolderAsync(string folderName)
    {
        // Recupera la Document Library
        DocumentLibrary library = await GetDefaultDocumentLibraryAsync();

        using HttpClient client = new();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _authenticationService.AccessToken);

        string json =
        $$"""
            {
                "name": "{{folderName}}",
                "folder": {},
                "@microsoft.graph.conflictBehavior": "fail"
            }
        """;

        using StringContent content =
            new(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response =
            await client.PostAsync(
                $"https://graph.microsoft.com/v1.0/drives/{library.Id}/root/children",
                content);

        response.EnsureSuccessStatusCode();
    }
}
