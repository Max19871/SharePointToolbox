using System;
using System.Collections.Generic;
using System.Text;

namespace SharePointToolbox.Models;

/// <summary>
/// Informazioni dell'utente autenticato.
/// </summary>
public class CurrentUser
{
    public string DisplayName { get; set; } = "";

    public string Mail { get; set; } = "";

    public string UserPrincipalName { get; set; } = "";

    public string Id { get; set; } = "";
}