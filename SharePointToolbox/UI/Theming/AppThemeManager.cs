using Microsoft.Extensions.Options;
using ReaLTaiizor.Colors;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using SharePointToolbox.Configuration;

namespace SharePointToolbox.UI.Theming;

public sealed class AppThemeManager
{
    private readonly MaterialSkinManager _manager = MaterialSkinManager.Instance;

    // Ruoli semantici usati dai controlli dei popup. I valori vengono sempre
    // caricati dalla sezione Theme di appsettings.json.
    public static Color PopupActiveText { get; private set; }
    public static Color PopupActiveLine { get; private set; }
    public static Color InstitutionalTopBorder { get; private set; }
    public static Color WindowControlBarBackground { get; private set; }

    public AppThemePalette Palette { get; }
    public AppThemeManager(IOptions<ThemeOptions> options)
    {
        Palette = AppThemePalette.FromOptions(options.Value);
        PopupActiveText = Palette.Primary;
        PopupActiveLine = Palette.InstitutionalAccent;
        InstitutionalTopBorder = Palette.InstitutionalAccent;
        WindowControlBarBackground = Palette.PrimaryDark;
        _manager.EnforceBackcolorOnAllComponents = true;
        _manager.Theme = Palette.IsDark
            ? MaterialSkinManager.Themes.DARK
            : MaterialSkinManager.Themes.LIGHT;
        _manager.ColorScheme = new MaterialColorScheme(
            ToMaterialValue<MaterialPrimary>(Palette.Primary),
            ToMaterialValue<MaterialPrimary>(Palette.PrimaryDark),
            ToMaterialValue<MaterialPrimary>(Palette.PrimaryLight),
            // Tutti i pulsanti con UseAccentColor rappresentano azioni critiche.
            ToMaterialValue<MaterialAccent>(Palette.Danger),
            UseLightText(Palette.Primary) ? MaterialTextShade.LIGHT : MaterialTextShade.DARK);
    }

    public void ApplyTo(MaterialForm form) => _manager.AddFormToManage(form);

    private static TEnum ToMaterialValue<TEnum>(Color color) where TEnum : struct, Enum =>
        (TEnum)Enum.ToObject(typeof(TEnum), color.ToArgb() & 0x00FFFFFF);

    private static bool UseLightText(Color background)
    {
        double luminance = (0.2126 * background.R + 0.7152 * background.G + 0.0722 * background.B) / 255;
        return luminance < 0.55;
    }
}
