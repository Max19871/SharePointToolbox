namespace SharePointToolbox.Configuration;

public sealed class BrandingOptions
{
    public string StudioName { get; set; } = "Giovanardi Studio Legale";
    public string ProductName { get; set; } = "Document Hub";
    public string LogoPath { get; set; } = "Resources/LogoStudio.png";
    public string Certifications { get; set; } = "ISO 9001, ISO/IEC 27001 e UNI 11871";

    public string ApplicationName => $"{StudioName} {ProductName}".Trim();
}
