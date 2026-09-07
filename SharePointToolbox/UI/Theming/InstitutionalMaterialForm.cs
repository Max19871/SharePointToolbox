using ReaLTaiizor.Forms;

namespace SharePointToolbox.UI.Theming;

/// <summary>
/// Base comune delle finestre Material dell'applicazione.
/// Mantiene uniforme il filetto istituzionale superiore.
/// </summary>
public abstract class InstitutionalMaterialForm : MaterialForm
{
    private const int TopBorderHeight = 4;
    private bool _backHovered;
    protected bool ShowBackNavigation { get; set; }

    protected InstitutionalMaterialForm()
    {
        string iconPath = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "AppIcon.ico");
        if (!File.Exists(iconPath)) return;

        using FileStream stream = File.OpenRead(iconPath);
        using var applicationIcon = new Icon(stream);
        Icon = (Icon)applicationIcon.Clone();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Color color = AppThemeManager.InstitutionalTopBorder.IsEmpty
            ? Color.FromArgb(201, 183, 106)
            : AppThemeManager.InstitutionalTopBorder;
        using var brush = new SolidBrush(color);
        e.Graphics.FillRectangle(brush, 0, 0, ClientSize.Width, TopBorderHeight);
        if (ShowBackNavigation)
            InstitutionalWindowControls.DrawBackNavigation(this, e.Graphics, _backHovered);
        else
            InstitutionalWindowControls.DrawInstitutionalTitle(
                this,
                e.Graphics,
                useEnvironmentAccent: true);
        InstitutionalWindowControls.Draw(this, e.Graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (ShowBackNavigation &&
            InstitutionalWindowControls.BackButtonBounds.Contains(e.Location))
        {
            OnBackNavigationRequested();
            return;
        }
        if (InstitutionalWindowControls.TryHandleClick(this, e.Location)) return;
        base.OnMouseDown(e);
    }

    protected virtual void OnBackNavigationRequested() => Close();

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hovered = ShowBackNavigation &&
                       InstitutionalWindowControls.BackButtonBounds.Contains(e.Location);
        if (_backHovered == hovered) return;
        _backHovered = hovered;
        Invalidate(InstitutionalWindowControls.BackButtonBounds);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (!_backHovered) return;
        _backHovered = false;
        Invalidate(InstitutionalWindowControls.BackButtonBounds);
    }
}

internal static class InstitutionalWindowControls
{
    private enum WindowCommand
    {
        None,
        Minimize,
        Maximize,
        Close
    }

    private sealed class WindowControlState
    {
        public WindowCommand HoveredCommand { get; set; }
        public bool TrackingAttached { get; set; }
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Form, WindowControlState>
        States = new();

    private const int StatusBarHeight = 24;
    private const int ActionBarHeight = 40;
    private const int BlueHeaderHeight = StatusBarHeight + ActionBarHeight;
    private const int TopBorderHeight = 4;
    private const int ButtonWidth = 48;
    private const int GlyphSize = 17;
    private const int RightInset = 0;
    private const int ButtonSpacing = 0;
    private const int InteractionHorizontalPadding = 0;
    private static readonly Lazy<Bitmap?> WindowButtonStrip = new(LoadWindowButtonStrip);
    private static readonly Lazy<Bitmap?> ReturnHomeStrip = new(LoadReturnHomeStrip);
    public static Rectangle BackButtonBounds =>
        new(0, TopBorderHeight, 48, BlueHeaderHeight - TopBorderHeight);

    public static void DrawBackNavigation(Form form, Graphics graphics, bool hovered)
    {
        DrawInstitutionalTitle(form, graphics, 56F, useEnvironmentAccent: true);
        Bitmap? strip = ReturnHomeStrip.Value;
        if (strip is not null)
        {
            int frameHeight = strip.Height / 2;
            Rectangle source = new(
                0,
                hovered ? frameHeight : 0,
                strip.Width,
                frameHeight);
            System.Drawing.Drawing2D.InterpolationMode previousInterpolation =
                graphics.InterpolationMode;
            graphics.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(
                strip,
                BackButtonBounds,
                source,
                GraphicsUnit.Pixel);
            graphics.InterpolationMode = previousInterpolation;
            return;
        }

        Color gold = AppThemeManager.InstitutionalTopBorder.IsEmpty
            ? Color.FromArgb(201, 183, 106)
            : AppThemeManager.InstitutionalTopBorder;
        System.Drawing.Drawing2D.SmoothingMode previousSmoothing = graphics.SmoothingMode;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var arrowPen = new Pen(gold, 3.6F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        int centerY = BackButtonBounds.Top + BackButtonBounds.Height / 2;
        int arrowTip = BackButtonBounds.Left + 10;
        int arrowBack = BackButtonBounds.Left + 20;
        int arrowEnd = BackButtonBounds.Left + 27;
        graphics.DrawLine(arrowPen, arrowBack, centerY - 8, arrowTip, centerY);
        graphics.DrawLine(arrowPen, arrowTip, centerY, arrowBack, centerY + 8);
        graphics.DrawLine(arrowPen, arrowTip, centerY, arrowEnd, centerY);
        graphics.SmoothingMode = previousSmoothing;
    }

    public static void DrawInstitutionalTitle(
        Form form,
        Graphics graphics,
        float titleX = 16F,
        bool useEnvironmentAccent = false)
    {
        Color background = AppThemeManager.PopupActiveText.IsEmpty
            ? Color.FromArgb(62, 88, 133)
            : AppThemeManager.PopupActiveText;
        using var backgroundBrush = new SolidBrush(background);
        graphics.FillRectangle(
            backgroundBrush,
            0,
            TopBorderHeight,
            form.ClientSize.Width,
            BlueHeaderHeight - TopBorderHeight);

        Color gold = AppThemeManager.InstitutionalTopBorder.IsEmpty
            ? Color.FromArgb(201, 183, 106)
            : AppThemeManager.InstitutionalTopBorder;
        System.Drawing.Drawing2D.SmoothingMode previousSmoothing = graphics.SmoothingMode;
        System.Drawing.Text.TextRenderingHint previousTextRendering = graphics.TextRenderingHint;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        using Font titleFont = AppTypography.Create(
            AppTypography.ApplicationTitleSize + 1.5F,
            FontStyle.Bold);
        const string studioName = "Giovanardi Studio Legale";
        string environment = useEnvironmentAccent &&
                             form.Text.StartsWith(studioName, StringComparison.OrdinalIgnoreCase)
            ? form.Text[studioName.Length..].TrimStart()
            : string.Empty;
        string prefix = string.IsNullOrWhiteSpace(environment)
            ? form.Text
            : $"{studioName} ";
        using var whiteBrush = new SolidBrush(Color.White);
        using var goldBrush = new SolidBrush(gold);
        using var format = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.NoWrap,
            Trimming = StringTrimming.EllipsisCharacter
        };
        float titleY = TopBorderHeight +
                       (BlueHeaderHeight - TopBorderHeight - titleFont.Height) / 2F -
                       1F;
        graphics.DrawString(prefix, titleFont, whiteBrush, titleX, titleY, format);
        if (!string.IsNullOrWhiteSpace(environment))
        {
            float prefixWidth = graphics.MeasureString(
                prefix,
                titleFont,
                int.MaxValue,
                format).Width;
            graphics.DrawString(
                environment,
                titleFont,
                goldBrush,
                titleX + prefixWidth,
                titleY,
                format);
        }
        graphics.SmoothingMode = previousSmoothing;
        graphics.TextRenderingHint = previousTextRendering;
    }

    public static void Draw(Form form, Graphics graphics)
    {
        if (!form.ControlBox) return;
        WindowControlState state = EnsureTracking(form);

        Color background = AppThemeManager.WindowControlBarBackground.IsEmpty
            ? Color.FromArgb(45, 73, 104)
            : AppThemeManager.WindowControlBarBackground;
        using var backgroundBrush = new SolidBrush(background);
        using var glyphPen = new Pen(Color.White, 2.6F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };

        int nativeButtonCount = 1 + (form.MaximizeBox ? 1 : 0) + (form.MinimizeBox ? 1 : 0);
        int nativeControlsLeft = form.ClientSize.Width - nativeButtonCount * ButtonWidth;
        // Cancella i piccoli glifi nativi dalla barra tecnica superiore prima
        // di ridisegnare quelli istituzionali nella posizione rientrata.
        graphics.FillRectangle(
            backgroundBrush,
            nativeControlsLeft,
            TopBorderHeight,
            nativeButtonCount * ButtonWidth,
            StatusBarHeight - TopBorderHeight);

        int closeLeft = form.ClientSize.Width - RightInset - ButtonWidth;
        if (!DrawWindowButtonSprite(
                graphics,
                closeLeft,
                spriteColumn: 1,
                hovered: state.HoveredCommand == WindowCommand.Close))
        {
            DrawHoverBackground(graphics, ButtonBounds(closeLeft), state.HoveredCommand, WindowCommand.Close);
            DrawClose(graphics, glyphPen, closeLeft);
        }

        int nextLeft = closeLeft - ButtonWidth - ButtonSpacing;
        if (form.MaximizeBox)
        {
            DrawHoverBackground(graphics, ButtonBounds(nextLeft), state.HoveredCommand, WindowCommand.Maximize);
            DrawMaximize(graphics, glyphPen, nextLeft);
            nextLeft -= ButtonWidth + ButtonSpacing;
        }

        if (form.MinimizeBox)
        {
            if (!DrawWindowButtonSprite(
                    graphics,
                    nextLeft,
                    spriteColumn: 0,
                    hovered: state.HoveredCommand == WindowCommand.Minimize))
            {
                DrawHoverBackground(graphics, ButtonBounds(nextLeft), state.HoveredCommand, WindowCommand.Minimize);
                DrawMinimize(graphics, glyphPen, nextLeft);
            }
        }
    }

    public static bool TryHandleClick(Form form, Point location)
    {
        if (!form.ControlBox || location.Y < TopBorderHeight || location.Y >= BlueHeaderHeight)
            return false;

        WindowCommand command = HitTest(form, location);
        if (command == WindowCommand.Close)
        {
            form.Close();
            return true;
        }
        if (command == WindowCommand.Maximize)
        {
            form.WindowState = form.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
            return true;
        }
        if (command == WindowCommand.Minimize)
        {
            form.WindowState = FormWindowState.Minimized;
            return true;
        }

        // Evita che le aree vuote tra i nuovi comandi vengano interpretate
        // come i pulsanti nativi sottostanti.
        int controlsLeft = LeftmostButtonLeft(form);
        if (location.X >= controlsLeft) return true;

        return false;
    }

    private static WindowControlState EnsureTracking(Form form)
    {
        WindowControlState state = States.GetOrCreateValue(form);
        if (state.TrackingAttached) return state;

        state.TrackingAttached = true;
        form.MouseMove += (_, e) =>
        {
            WindowCommand command = HitTest(form, e.Location);
            if (state.HoveredCommand == command) return;
            state.HoveredCommand = command;
            form.Invalidate(new Rectangle(
                Math.Max(0, LeftmostButtonLeft(form)),
                TopBorderHeight,
                form.ClientSize.Width - Math.Max(0, LeftmostButtonLeft(form)),
                BlueHeaderHeight - TopBorderHeight));
        };
        form.MouseLeave += (_, _) =>
        {
            if (state.HoveredCommand == WindowCommand.None) return;
            state.HoveredCommand = WindowCommand.None;
            form.Invalidate();
        };
        return state;
    }

    private static WindowCommand HitTest(Form form, Point location)
    {
        if (location.Y < TopBorderHeight || location.Y >= BlueHeaderHeight)
            return WindowCommand.None;

        int closeLeft = form.ClientSize.Width - RightInset - ButtonWidth;
        if (ButtonBounds(closeLeft).Contains(location)) return WindowCommand.Close;

        int nextLeft = closeLeft - ButtonWidth - ButtonSpacing;
        if (form.MaximizeBox)
        {
            if (ButtonBounds(nextLeft).Contains(location)) return WindowCommand.Maximize;
            nextLeft -= ButtonWidth + ButtonSpacing;
        }

        return form.MinimizeBox && ButtonBounds(nextLeft).Contains(location)
            ? WindowCommand.Minimize
            : WindowCommand.None;
    }

    private static int LeftmostButtonLeft(Form form)
    {
        int left = form.ClientSize.Width - RightInset - ButtonWidth;
        if (form.MaximizeBox) left -= ButtonWidth + ButtonSpacing;
        if (form.MinimizeBox) left -= ButtonWidth + ButtonSpacing;
        return left - InteractionHorizontalPadding;
    }

    private static Rectangle ButtonBounds(int left) =>
        new(
            left - InteractionHorizontalPadding,
            TopBorderHeight,
            ButtonWidth + InteractionHorizontalPadding * 2,
            BlueHeaderHeight - TopBorderHeight);

    private static void DrawHoverBackground(
        Graphics graphics,
        Rectangle bounds,
        WindowCommand hovered,
        WindowCommand command)
    {
        if (hovered != command) return;
        Color color = Color.FromArgb(74, 112, 158);
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, bounds);
    }

    private static bool DrawWindowButtonSprite(
        Graphics graphics,
        int left,
        int spriteColumn,
        bool hovered)
    {
        Bitmap? strip = WindowButtonStrip.Value;
        if (strip is null) return false;

        int frameWidth = strip.Width / 2;
        int frameHeight = strip.Height / 2;
        Rectangle source = new(
            spriteColumn * frameWidth,
            hovered ? frameHeight : 0,
            frameWidth,
            frameHeight);
        Rectangle destination = new(
            left,
            TopBorderHeight,
            frameWidth,
            frameHeight);
        System.Drawing.Drawing2D.InterpolationMode previousInterpolation =
            graphics.InterpolationMode;
        graphics.InterpolationMode =
            System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(strip, destination, source, GraphicsUnit.Pixel);
        graphics.InterpolationMode = previousInterpolation;
        return true;
    }

    private static Bitmap? LoadWindowButtonStrip()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "WindowControls.png");
        if (!File.Exists(path)) return null;
        using Image source = Image.FromFile(path);
        return new Bitmap(source);
    }

    private static Bitmap? LoadReturnHomeStrip()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "ReturnHome.png");
        if (!File.Exists(path)) return null;
        using Image source = Image.FromFile(path);
        return new Bitmap(source);
    }

    private static void DrawClose(Graphics graphics, Pen pen, int left)
    {
        Rectangle bounds = GlyphBounds(left);
        graphics.DrawLine(pen, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
        graphics.DrawLine(pen, bounds.Right, bounds.Top, bounds.Left, bounds.Bottom);
    }

    private static void DrawMinimize(Graphics graphics, Pen pen, int left)
    {
        Rectangle bounds = GlyphBounds(left);
        graphics.DrawLine(pen, bounds.Left, bounds.Bottom, bounds.Right, bounds.Bottom);
    }

    private static void DrawMaximize(Graphics graphics, Pen pen, int left)
    {
        Rectangle bounds = GlyphBounds(left);
        graphics.DrawRectangle(pen, bounds);
    }

    private static Rectangle GlyphBounds(int buttonLeft)
    {
        int x = buttonLeft + ((ButtonWidth - GlyphSize) / 2);
        int availableHeight = BlueHeaderHeight - TopBorderHeight;
        int y = TopBorderHeight + ((availableHeight - GlyphSize) / 2);
        return new Rectangle(x, y, GlyphSize, GlyphSize);
    }
}
