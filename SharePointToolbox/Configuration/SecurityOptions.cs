namespace SharePointToolbox.Configuration;

public sealed class SecurityOptions
{
    public string ApplicationUsersGroupId { get; set; } = string.Empty;
    public string AuditReadersGroupId { get; set; } = string.Empty;
}
