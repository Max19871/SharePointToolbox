using System;
using System.Collections.Generic;
using System.Text;

namespace SharePointToolbox.Models;

/// <summary>
/// Informazioni del sito SharePoint.
/// </summary>
public class SharePointSite
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string WebUrl { get; set; } = "";
}