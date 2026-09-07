using ReaLTaiizor.Controls;

namespace SharePointToolbox.UI.Theming;

internal static class InstitutionalUnderline
{
    public static void Draw(Control control, PaintEventArgs e, int nativeBottomPadding = 2)
    {
        int thickness = control.Focused || control.ContainsFocus ? 4 : 2;
        float scale = control.DeviceDpi / 96F;
        int nativeLineY = control.ClientSize.Height - Math.Max(1, (int)Math.Round(nativeBottomPadding * scale));
        int y = Math.Max(0, nativeLineY - Math.Max(0, thickness - 2));
        using var brush = new SolidBrush(AppThemeManager.PopupActiveLine);
        e.Graphics.FillRectangle(brush, 0, y, control.ClientSize.Width, thickness);
    }
}

internal sealed class InstitutionalMaterialTextBoxEdit : MaterialTextBoxEdit
{
    public InstitutionalMaterialTextBoxEdit() => UseAccent = false;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        InstitutionalUnderline.Draw(this, e);
    }
}

internal sealed class InstitutionalMaterialMultiLineTextBoxEdit : MaterialMultiLineTextBoxEdit
{
    public InstitutionalMaterialMultiLineTextBoxEdit() => UseAccent = false;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        InstitutionalUnderline.Draw(this, e, nativeBottomPadding: 3);
    }
}

internal sealed class InstitutionalMaterialComboBox : MaterialComboBox
{
    public InstitutionalMaterialComboBox() => UseAccent = false;

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        InstitutionalUnderline.Draw(this, e);
    }
}
