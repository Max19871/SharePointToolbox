using System;
using System.Collections.Generic;
using System.Text;

namespace SharePointToolbox.Configuration;

/// <summary>
/// Contiene i parametri di autenticazione Microsoft Entra ID
/// letti dal file appsettings.json.
/// </summary>
public class AzureAdOptions
{
    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;
}