namespace SharePointToolbox.Configuration;

public class MailOptions
{
    public string SharedMailboxAddress { get; set; } = string.Empty;
    public string SharedMailboxObjectId { get; set; } = string.Empty;
    public string CreationNotificationAddress { get; set; } = string.Empty;
    public string CreationNotificationSubject { get; set; } =
        "Nuova condivisione Data Room: {Condivisione}";
    public string SubjectTemplate { get; set; } = "Accesso alla condivisione {Condivisione}";
    public string ProductLabel { get; set; } = "DATA ROOM";
    public string Introduction { get; set; } = string.Empty;
    public string ButtonText { get; set; } = "APRI LA CONDIVISIONE";
    public string AccessNotice { get; set; } = string.Empty;
    public string ConfidentialityTitle { get; set; } = "Riservatezza";
    public string ConfidentialityText { get; set; } = string.Empty;
    public string OfficeTitle { get; set; } = string.Empty;
    public string OfficeAddress { get; set; } = string.Empty;
    public string OfficePhone { get; set; } = string.Empty;
    public string SupportText { get; set; } = string.Empty;
    public string SupportAddress { get; set; } = string.Empty;
    public string LogoPath { get; set; } = "Resources/LogoStudio.png";
    public string WatermarkPath { get; set; } = "Resources/EmailBuildingWatermark.png";
    public string TemplatePath { get; set; } = "Resources/EmailInvitationTemplate.html";
}
