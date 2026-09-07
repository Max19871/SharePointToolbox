namespace SharePointToolbox.Configuration;

public sealed class PressReviewOptions
{
    public string SiteUrl { get; set; } = string.Empty;
    public string LibraryName { get; set; } = "Documenti condivisi";
    public string NotificationRecipient { get; set; } = string.Empty;
    public string SenderMailbox { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = "Rassegna stampa – {DataRassegna}";
    public string TemplatePath { get; set; } = "Resources/EmailInvitationTemplate.html";
    public string LogoPath { get; set; } = "Resources/LogoStudio.png";
    public string WatermarkPath { get; set; } = "Resources/EmailBuildingWatermark.png";
}
