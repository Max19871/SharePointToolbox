using System;
using System.Collections.Generic;
using System.Text;

namespace SharePointToolbox.Models;

/// <summary>
/// Rappresenta una Document Library di SharePoint.
/// </summary>
public class DocumentLibrary
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string WebUrl { get; set; } = "";
}