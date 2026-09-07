namespace SharePointToolbox.Configuration;

public sealed class ThemeOptions
{
    public string Mode { get; set; } = "Light";
    public string Primary { get; set; } = "#3E5885";
    public string PrimaryDark { get; set; } = "#3E5885";
    public string PrimaryLight { get; set; } = "#D6E2EE";
    public string InstitutionalAccent { get; set; } = "#C9B76A";
    public string Accent { get; set; } = "#B44B79";
    public string Danger { get; set; } = "#B44B79";
    public string Success { get; set; } = "#2E7D32";
    public string Warning { get; set; } = "#ED6C02";
    public string SurfaceLight { get; set; } = "#FAFAFA";
    public string SurfaceDark { get; set; } = "#515151";
    public string TextLight { get; set; } = "#212121";
    public string TextDark { get; set; } = "#FFFFFF";
    public string SecondaryTextLight { get; set; } = "#555555";
    public string SecondaryTextDark { get; set; } = "#D0D0D0";
    public string DashboardButtonBackground { get; set; } = "#E2E2E2";
    public string DashboardButtonForeground { get; set; } = "#212121";
}
