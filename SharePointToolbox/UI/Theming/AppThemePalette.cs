using SharePointToolbox.Configuration;
using System.Drawing;

namespace SharePointToolbox.UI.Theming;

public sealed record AppThemePalette(
    Color Primary,
    Color PrimaryDark,
    Color PrimaryLight,
    Color InstitutionalAccent,
    Color Accent,
    Color Danger,
    Color Success,
    Color Warning,
    Color SurfaceLight,
    Color SurfaceDark,
    Color TextLight,
    Color TextDark,
    Color SecondaryTextLight,
    Color SecondaryTextDark,
    Color DashboardButtonBackground,
    Color DashboardButtonForeground,
    bool IsDark)
{
    public static AppThemePalette FromOptions(ThemeOptions options) => new(
        Parse(options.Primary, nameof(options.Primary)),
        Parse(options.PrimaryDark, nameof(options.PrimaryDark)),
        Parse(options.PrimaryLight, nameof(options.PrimaryLight)),
        Parse(options.InstitutionalAccent, nameof(options.InstitutionalAccent)),
        Parse(options.Accent, nameof(options.Accent)),
        Parse(options.Danger, nameof(options.Danger)),
        Parse(options.Success, nameof(options.Success)),
        Parse(options.Warning, nameof(options.Warning)),
        Parse(options.SurfaceLight, nameof(options.SurfaceLight)),
        Parse(options.SurfaceDark, nameof(options.SurfaceDark)),
        Parse(options.TextLight, nameof(options.TextLight)),
        Parse(options.TextDark, nameof(options.TextDark)),
        Parse(options.SecondaryTextLight, nameof(options.SecondaryTextLight)),
        Parse(options.SecondaryTextDark, nameof(options.SecondaryTextDark)),
        Parse(options.DashboardButtonBackground, nameof(options.DashboardButtonBackground)),
        Parse(options.DashboardButtonForeground, nameof(options.DashboardButtonForeground)),
        options.Mode.Equals("Dark", StringComparison.OrdinalIgnoreCase));

    private static Color Parse(string value, string optionName)
    {
        try
        {
            Color color = ColorTranslator.FromHtml(value);
            if (color.IsEmpty) throw new ArgumentException();
            return color;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            throw new InvalidOperationException(
                $"Il colore Theme:{optionName} ('{value}') non è valido. Usa il formato #RRGGBB.",
                ex);
        }
    }
}
