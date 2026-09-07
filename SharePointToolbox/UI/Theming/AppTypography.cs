namespace SharePointToolbox.UI.Theming;

/// <summary>
/// Unico punto di configurazione della tipografia dell'applicazione.
/// Le dimensioni restano espresse semanticamente nei singoli controlli,
/// mentre famiglia e costruzione dei font sono uniformi.
/// </summary>
public static class AppTypography
{
    public const string FamilyName = "Calibri";

    public const float ApplicationTitleSize = 16F;
    public const float BodySize = 10F;
    public const float ButtonSize = 9.5F;

    public static Font Create(
        float size,
        FontStyle style = FontStyle.Regular,
        GraphicsUnit unit = GraphicsUnit.Point) =>
        new(FamilyName, size, style, unit);
}
