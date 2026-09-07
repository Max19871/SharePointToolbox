using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using SharePointToolbox.Configuration;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.UI.Theming;
using System.Net.Mail;
using System.Runtime.InteropServices;

namespace SharePointToolbox.UI;

public abstract class AppDialogForm : InstitutionalMaterialForm
{
    protected AppDialogForm(string title, Size clientSize)
    {
        Text = title;
        ClientSize = clientSize;
        // Tutti i requester applicativi devono avere una posizione prevedibile,
        // anche quando la finestra che li ha generati si sta chiudendo.
        StartPosition = FormStartPosition.CenterScreen;
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        MaterialSkinManager.Instance.AddFormToManage(this);
        Shown += (_, _) => BeginInvoke(ResetButtonVisualStates);
        Activated += (_, _) => BeginInvoke(ResetButtonVisualStates);
    }

    private void ResetButtonVisualStates()
    {
        if (IsDisposed) return;
        ActiveControl = null;
        foreach (MaterialButton button in FindButtons(this))
        {
            System.Reflection.PropertyInfo? mouseStateProperty =
                button.GetType().GetProperty(nameof(button.MouseState));
            if (mouseStateProperty?.PropertyType.IsEnum == true)
            {
                object outsideState = Enum.Parse(mouseStateProperty.PropertyType, "OUT");
                mouseStateProperty.SetValue(button, outsideState);
            }
            button.Invalidate();
        }
    }

    private static IEnumerable<MaterialButton> FindButtons(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (child is MaterialButton button) yield return button;
            foreach (MaterialButton nested in FindButtons(child)) yield return nested;
        }
    }

    protected static MaterialButton Button(string text, bool accent = false, bool outlined = false) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(UiMetrics.ButtonWidth, UiMetrics.ButtonHeight),
        Type = outlined ? MaterialButton.MaterialButtonType.Outlined : MaterialButton.MaterialButtonType.Contained,
        HighEmphasis = true,
        UseAccentColor = accent
    };

    protected static Label Caption(string text, Point location, Size size, bool bold = false) => new()
    {
        Text = text,
        Location = location,
        Size = size,
        Font = AppTypography.Create(10F, bold ? FontStyle.Bold : FontStyle.Regular),
        BackColor = Color.Transparent
    };
}

internal sealed class FixedFontLabel : Label
{
    private readonly Font _displayFont;

    public FixedFontLabel(string text, Point location, Size size, float fontSize, FontStyle style)
    {
        Text = text;
        Location = location;
        Size = size;
        _displayFont = AppTypography.Create(fontSize, style);
        Font = _displayFont;
        BackColor = Color.Transparent;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        TextRenderer.DrawText(
            e.Graphics,
            Text,
            _displayFont,
            ClientRectangle,
            ForeColor,
            TextFormatFlags.NoPadding
            | TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _displayFont.Dispose();
        base.Dispose(disposing);
    }
}

internal sealed class SpacedMessageLabel : Control
{
    private const int LineGap = 5;
    private readonly Font _displayFont = AppTypography.Create(10.75F);

    public SpacedMessageLabel(string text)
    {
        Text = text;
        Font = _displayFont;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.SupportsTransparentBackColor
            | ControlStyles.UserPaint,
            true);
        BackColor = Color.Transparent;
    }

    public int MeasureRequiredHeight(int availableWidth)
    {
        IReadOnlyList<string> lines = WrapLines(Math.Max(1, availableWidth));
        int lineHeight = TextRenderer.MeasureText(
            "Ag",
            _displayFont,
            Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Height;
        return lines.Count * lineHeight + Math.Max(0, lines.Count - 1) * LineGap;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        IReadOnlyList<string> lines = WrapLines(Math.Max(1, ClientSize.Width));
        int lineHeight = TextRenderer.MeasureText(
            "Ag",
            _displayFont,
            Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Height;
        int textHeight = lines.Count * lineHeight + Math.Max(0, lines.Count - 1) * LineGap;
        int top = Math.Max(0, (ClientSize.Height - textHeight) / 2);
        foreach (string line in lines)
        {
            TextRenderer.DrawText(
                e.Graphics,
                line,
                _displayFont,
                new Rectangle(0, top, ClientSize.Width, lineHeight),
                ForeColor,
                TextFormatFlags.Left
                | TextFormatFlags.NoPadding
                | TextFormatFlags.NoPrefix
                | TextFormatFlags.SingleLine);
            top += lineHeight + LineGap;
        }
    }

    private IReadOnlyList<string> WrapLines(int availableWidth)
    {
        var lines = new List<string>();
        string normalized = Text.Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (string paragraph in normalized.Split('\n'))
        {
            string[] words = paragraph.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                lines.Add(string.Empty);
                continue;
            }

            string current = words[0];
            for (int index = 1; index < words.Length; index++)
            {
                string candidate = $"{current} {words[index]}";
                int candidateWidth = TextRenderer.MeasureText(
                    candidate,
                    _displayFont,
                    Size.Empty,
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
                if (candidateWidth <= availableWidth)
                {
                    current = candidate;
                }
                else
                {
                    lines.Add(current);
                    current = words[index];
                }
            }
            lines.Add(current);
        }
        return lines;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _displayFont.Dispose();
        base.Dispose(disposing);
    }
}

public static class AppMessageBox
{
    public static DialogResult Show(
        string message,
        string title,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        IWin32Window? owner = null)
    {
        using var dialog = new AppMessageForm(message, title, buttons, icon);
        return dialog.ShowDialog(owner ?? Form.ActiveForm);
    }
}

public sealed class AppMessageForm : AppDialogForm
{
    public AppMessageForm(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        : base(title, new Size(500, 250))
    {
        int textLeft = icon == MessageBoxIcon.None ? 28 : 134;
        Control? messageIcon = null;
        if (icon != MessageBoxIcon.None)
        {
            messageIcon = new MessageGlyphIcon(icon)
            {
                Size = new Size(88, 77),
                BackColor = Color.Transparent
            };
            Controls.Add(messageIcon);
        }

        const int contentTop = 78;
        int textWidth = ClientSize.Width - textLeft - 24;
        var messageLabel = new SpacedMessageLabel(message)
        {
            Location = new Point(textLeft, contentTop),
            Size = new Size(textWidth, 48)
        };
        messageLabel.Height = Math.Max(
            48,
            messageLabel.MeasureRequiredHeight(textWidth) + 12);
        Controls.Add(messageLabel);
        int contentHeight = Math.Max(
            messageLabel.Height,
            messageIcon?.Height ?? 0);
        messageLabel.Top = contentTop + (contentHeight - messageLabel.Height) / 2;
        if (messageIcon is not null)
            messageIcon.Location = new Point(
                24,
                contentTop + (contentHeight - messageIcon.Height) / 2);

        int actionsTop = contentTop + contentHeight + UiMetrics.Spacing;
        var actions = new System.Windows.Forms.Panel
        {
            Bounds = new Rectangle(
                UiMetrics.Spacing,
                actionsTop,
                ClientSize.Width - UiMetrics.Spacing * 2,
                UiMetrics.ButtonHeight + UiMetrics.ShadowClearance),
            BackColor = Color.Transparent
        };
        if (buttons == MessageBoxButtons.YesNo)
        {
            MaterialButton yes = Button("Sì");
            MaterialButton no = Button("No", outlined: true);
            yes.DialogResult = DialogResult.Yes;
            no.DialogResult = DialogResult.No;
            int groupWidth = UiMetrics.ButtonWidth * 2 + UiMetrics.Spacing;
            no.Location = new Point((actions.ClientSize.Width - groupWidth) / 2, 0);
            yes.Location = new Point(no.Right + UiMetrics.Spacing, 0);
            actions.Controls.Add(no);
            actions.Controls.Add(yes);
            AcceptButton = yes;
            CancelButton = no;
        }
        else
        {
            MaterialButton ok = Button("OK");
            ok.DialogResult = DialogResult.OK;
            ok.Location = new Point((actions.ClientSize.Width - ok.Width) / 2, 0);
            actions.Controls.Add(ok);
            AcceptButton = ok;
            CancelButton = ok;
        }
        Controls.Add(actions);
        ClientSize = new Size(
            ClientSize.Width,
            actions.Bottom + UiMetrics.Spacing);
    }

    private sealed class MessageGlyphIcon : Control
    {
        private readonly MessageBoxIcon _icon;
        private readonly Bitmap? _iconStrip;

        public MessageGlyphIcon(MessageBoxIcon icon)
        {
            _icon = icon;
            string stripPath = Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "AlertIcons.png");
            if (File.Exists(stripPath))
            {
                using Image source = Image.FromFile(stripPath);
                _iconStrip = new Bitmap(source);
            }
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (_iconStrip is not null)
            {
                e.Graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode =
                    System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                Rectangle source = _icon switch
                {
                    MessageBoxIcon.Warning => new Rectangle(0, 0, 88, 77),
                    MessageBoxIcon.Question => new Rectangle(93, 0, 78, 77),
                    MessageBoxIcon.Error => new Rectangle(270, 0, 77, 77),
                    _ => new Rectangle(181, 0, 78, 77)
                };
                float scale = Math.Min(
                    Width / (float)source.Width,
                    Height / (float)source.Height);
                int width = (int)Math.Round(source.Width * scale);
                int height = (int)Math.Round(source.Height * scale);
                Rectangle destination = new(
                    (Width - width) / 2,
                    (Height - height) / 2,
                    width,
                    height);
                e.Graphics.DrawImage(
                    _iconStrip,
                    destination,
                    source,
                    GraphicsUnit.Pixel);
                return;
            }

            Color color = AppThemeManager.PopupActiveText.IsEmpty
                ? Color.FromArgb(62, 88, 133)
                : AppThemeManager.PopupActiveText;
            float stroke = Math.Max(3F, Width / 15F);
            using var pen = new Pen(color, stroke)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            float inset = stroke;
            RectangleF bounds = new(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);

            if (_icon == MessageBoxIcon.Warning)
            {
                PointF top = new(Width / 2F, inset + 1);
                PointF left = new(inset + 1, Height - inset - 2);
                PointF right = new(Width - inset - 2, Height - inset - 2);
                using System.Drawing.Drawing2D.GraphicsPath triangle = RoundedTriangle(top, right, left, 6F);
                e.Graphics.DrawPath(pen, triangle);
                DrawCenteredGlyph(e.Graphics, "!", color, 24F, new RectangleF(0, 12, Width, Height - 10));
                return;
            }

            if (_icon == MessageBoxIcon.Question)
            {
                using var circleBrush = new SolidBrush(color);
                e.Graphics.FillEllipse(circleBrush, bounds);
                DrawGeometricallyCenteredGlyph(
                    e.Graphics,
                    "?",
                    ResolvePopupBackground(),
                    bounds);
                return;
            }

            e.Graphics.DrawEllipse(pen, bounds);
            if (_icon == MessageBoxIcon.Error)
            {
                float armInset = Width * 0.30F;
                e.Graphics.DrawLine(pen, armInset, armInset, Width - armInset, Height - armInset);
                e.Graphics.DrawLine(pen, Width - armInset, armInset, armInset, Height - armInset);
            }
            else
            {
                DrawCenteredGlyph(e.Graphics, "i", color, 25F, ClientRectangle);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _iconStrip?.Dispose();
            base.Dispose(disposing);
        }

        private static void DrawCenteredGlyph(
            Graphics graphics,
            string glyph,
            Color color,
            float size,
            RectangleF bounds)
        {
            using var font = AppTypography.Create(size, FontStyle.Bold, GraphicsUnit.Pixel);
            using var brush = new SolidBrush(color);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString(glyph, font, brush, bounds, format);
        }

        private static void DrawGeometricallyCenteredGlyph(
            Graphics graphics,
            string glyph,
            Color color,
            RectangleF bounds)
        {
            const float templateSize = 100F;
            using var font = AppTypography.Create(templateSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using var family = font.FontFamily;
            using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddString(
                glyph,
                family,
                (int)FontStyle.Bold,
                templateSize,
                PointF.Empty,
                format);

            RectangleF glyphBounds = path.GetBounds();
            float scale = Math.Min(
                bounds.Width * 0.70F / glyphBounds.Width,
                bounds.Height * 0.90F / glyphBounds.Height);
            float fittedWidth = glyphBounds.Width * scale;
            float fittedHeight = glyphBounds.Height * scale;
            float targetLeft = bounds.Left + (bounds.Width - fittedWidth) / 2F + 2F;
            float targetTop = bounds.Top + (bounds.Height - fittedHeight) / 2F;
            using var transform = new System.Drawing.Drawing2D.Matrix(
                scale,
                0,
                0,
                scale,
                targetLeft - glyphBounds.Left * scale,
                targetTop - glyphBounds.Top * scale);
            path.Transform(transform);

            using var brush = new SolidBrush(color);
            graphics.FillPath(brush, path);
        }

        private Color ResolvePopupBackground()
        {
            Control? current = Parent;
            while (current is not null)
            {
                if (current.BackColor != Color.Transparent && current.BackColor.A == 255)
                    return current.BackColor;
                current = current.Parent;
            }
            return Color.FromArgb(245, 245, 245);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedTriangle(
            PointF top,
            PointF right,
            PointF left,
            float radius)
        {
            PointF topFromLeft = Towards(top, left, radius);
            PointF topToRight = Towards(top, right, radius);
            PointF rightFromTop = Towards(right, top, radius);
            PointF rightToLeft = Towards(right, left, radius);
            PointF leftFromRight = Towards(left, right, radius);
            PointF leftToTop = Towards(left, top, radius);

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.StartFigure();
            path.AddBezier(topFromLeft, top, top, topToRight);
            path.AddLine(topToRight, rightFromTop);
            path.AddBezier(rightFromTop, right, right, rightToLeft);
            path.AddLine(rightToLeft, leftFromRight);
            path.AddBezier(leftFromRight, left, left, leftToTop);
            path.AddLine(leftToTop, topFromLeft);
            path.CloseFigure();
            return path;
        }

        private static PointF Towards(PointF origin, PointF target, float distance)
        {
            float dx = target.X - origin.X;
            float dy = target.Y - origin.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);
            return length <= 0F
                ? origin
                : new PointF(origin.X + dx / length * distance, origin.Y + dy / length * distance);
        }
    }
}

public sealed class ApplicationAccessDeniedForm : AppDialogForm
{
    public ApplicationAccessDeniedForm(string accountEmail, bool configurationMissing = false)
        : base(
            configurationMissing ? "Configurazione sicurezza incompleta" : "Accesso non autorizzato",
            new Size(500, 293))
    {
        var goldDivider = new System.Windows.Forms.Panel
        {
            Bounds = new Rectangle(0, 64, ClientSize.Width, 3),
            BackColor = AppThemeManager.PopupActiveLine,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        Controls.Add(goldDivider);

        var icon = new SecurityShieldIcon
        {
            Bounds = new Rectangle(22, 98, 82, 94)
        };
        Controls.Add(icon);

        string heading = configurationMissing
            ? "Impossibile verificare gli utenti autorizzati"
            : "Questo account non può utilizzare l’applicazione";
        string explanation = configurationMissing
            ? "L’identificativo del gruppo Microsoft 365 degli utilizzatori non è configurato. "
              + "Per motivi di sicurezza l’applicazione non può proseguire."
            : "L’account Microsoft 365 è valido, ma non appartiene al gruppo di sicurezza "
              + "autorizzato all’utilizzo dell’applicazione.";

        Controls.Add(new FixedFontLabel(
            heading,
            new Point(120, 82),
            new Size(356, 30),
            10.5F,
            FontStyle.Bold));
        Label explanationLabel = Caption(explanation, new Point(120, 115), new Size(356, 56));
        explanationLabel.Font = AppTypography.Create(10F);
        Controls.Add(explanationLabel);

        Controls.Add(Caption("Account:", new Point(120, 177), new Size(70, 24), bold: true));
        Label accountValue = Caption(accountEmail, new Point(190, 177), new Size(286, 24));
        accountValue.Font = AppTypography.Create(10F);
        accountValue.ForeColor = AppThemeManager.PopupActiveText;
        Controls.Add(accountValue);
        Label contactLabel = Caption(
            "Contatta l’amministratore Microsoft 365 dello Studio.",
            new Point(120, 203),
            new Size(356, 22));
        contactLabel.Font = AppTypography.Create(10F);
        Controls.Add(contactLabel);

        MaterialButton exit = Button("Esci dall’app");
        exit.Width = 170;
        exit.Location = new Point(
            (ClientSize.Width - exit.Width) / 2,
            ClientSize.Height - UiMetrics.Spacing - exit.Height);
        exit.DialogResult = DialogResult.OK;
        Controls.Add(exit);
        AcceptButton = exit;
        CancelButton = exit;
    }
}

internal sealed class SecurityShieldIcon : Control
{
    public SecurityShieldIcon()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw
            | ControlStyles.SupportsTransparentBackColor
            | ControlStyles.UserPaint,
            true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var shield = new System.Drawing.Drawing2D.GraphicsPath();
        shield.StartFigure();
        shield.AddLine(Width / 2F, 3, Width - 7, 18);
        shield.AddLine(Width - 7, 18, Width - 11, Height * 0.61F);
        shield.AddBezier(
            Width - 11, Height * 0.61F,
            Width - 14, Height * 0.79F,
            Width * 0.67F, Height - 8,
            Width / 2F, Height - 3);
        shield.AddBezier(
            Width / 2F, Height - 3,
            Width * 0.33F, Height - 8,
            14, Height * 0.79F,
            11, Height * 0.61F);
        shield.AddLine(11, Height * 0.61F, 7, 18);
        shield.CloseFigure();

        using var shieldBrush = new SolidBrush(AppThemeManager.PopupActiveText);
        using var whitePen = new Pen(Color.White, 3F);
        e.Graphics.FillPath(shieldBrush, shield);
        e.Graphics.DrawPath(whitePen, shield);

        Rectangle lockBody = new(
            (int)(Width * 0.31F),
            (int)(Height * 0.46F),
            (int)(Width * 0.38F),
            (int)(Height * 0.27F));
        using var lockBrush = new SolidBrush(Color.White);
        e.Graphics.FillRoundedRectangle(lockBrush, lockBody, new Size(6, 6));
        using var lockPen = new Pen(Color.White, 5F);
        e.Graphics.DrawArc(
            lockPen,
            Width * 0.36F,
            Height * 0.30F,
            Width * 0.28F,
            Height * 0.31F,
            180,
            180);
    }
}

public sealed class AboutForm : AppDialogForm
{
    public AboutForm(BrandingOptions branding, bool acknowledgementRequired = false)
        : base("Informazioni", new Size(740, 494))
    {
        var card = new MaterialCard
        {
            Bounds = new Rectangle(18, 78, 704, 344),
            Padding = new Padding(22)
        };
        var logo = new PictureBox
        {
            // Il logo è centrato rispetto all'intero blocco informativo alla sua destra.
            Bounds = new Rectangle(27, 37, 175, 66),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent
        };
        string logoPath = Path.Combine(AppContext.BaseDirectory, branding.LogoPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(logoPath))
        {
            using Image source = Image.FromFile(logoPath);
            logo.Image = new Bitmap(source);
        }
        card.Controls.Add(logo);
        Label applicationName = new FixedFontLabel(
            branding.ApplicationName,
            new Point(225, 27),
            new Size(455, 32),
            14F,
            FontStyle.Bold);
        card.Controls.Add(applicationName);
        card.Controls.Add(Caption(
            $"© 2026 {branding.StudioName}. Tutti i diritti riservati.",
            new Point(225, 62), new Size(455, 20)));
        card.Controls.Add(Caption(
            $"Versione {Application.ProductVersion.Split('+')[0]}",
            new Point(225, 90), new Size(455, 20)));
        Label securityTitle = new FixedFontLabel(
            "Sicurezza e uso autorizzato",
            new Point(22, 144),
            new Size(660, 26),
            11F,
            FontStyle.Bold);
        card.Controls.Add(securityTitle);
        card.Controls.Add(Caption(AppConstants.DisclaimerText, new Point(22, 177), new Size(660, 155)));
        Controls.Add(card);

        MaterialButton close = Button(acknowledgementRequired ? "Ho letto e compreso" : "Chiudi");
        if (acknowledgementRequired) close.Width = 190;
        int footerHeight = ClientSize.Height - card.Bottom;
        close.Location = new Point(
            (ClientSize.Width - close.Width) / 2,
            card.Bottom + (footerHeight - close.Height) / 2);
        close.DialogResult = DialogResult.OK;
        Controls.Add(close);
        AcceptButton = close;
        CancelButton = close;
    }
}

public sealed record ExpiredPracticeInfo(string Id, string Name, DateTimeOffset ExpirationDate);

public sealed class ExpiredPracticesForm : AppDialogForm
{
    private readonly ListView _practices = new();
    private readonly IReadOnlyList<ExpiredPracticeInfo> _items;
    public IReadOnlyList<string> PracticeIdsToClose { get; private set; } = Array.Empty<string>();
    public string PracticeIdToOpen { get; private set; } = string.Empty;

    public ExpiredPracticesForm(IReadOnlyList<ExpiredPracticeInfo> practices)
        : base("Condivisioni scadute", new Size(720, 470))
    {
        _items = practices;
        Controls.Add(Caption($"Sono state rilevate {practices.Count} condivisioni con scadenza superata.", new Point(24, 78), new Size(650, 25), true));
        _practices.Bounds = new Rectangle(24, 115, 672, 270);
        _practices.View = View.Details;
        _practices.FullRowSelect = true;
        _practices.MultiSelect = false;
        _practices.BackColor = Color.White;
        _practices.ForeColor = Color.FromArgb(32, 32, 32);
        _practices.Columns.Add("Condivisione", 480);
        _practices.Columns.Add("Scaduta il", 160);
        InstitutionalSelectionStyle.Apply(_practices);
        _practices.HandleCreated += (_, _) => EnforcePracticesBackground();
        _practices.BackColorChanged += (_, _) =>
        {
            if (_practices.BackColor != Color.White)
                EnforcePracticesBackground();
        };
        Shown += (_, _) => EnforcePracticesBackground();
        Activated += (_, _) => EnforcePracticesBackground();
        foreach (ExpiredPracticeInfo practice in practices)
        {
            var item = new ListViewItem(practice.Name) { Tag = practice.Id };
            item.SubItems.Add(practice.ExpirationDate.LocalDateTime.ToString("dd/MM/yyyy"));
            _practices.Items.Add(item);
        }
        Controls.Add(_practices);

        MaterialButton later = Button("Non ora", outlined: true);
        MaterialButton open = Button("Apri condivisione", outlined: true);
        MaterialButton selected = Button("Chiudi selezionata", outlined: true);
        MaterialButton all = Button("Chiudi tutte", accent: true);
        open.Width = UiMetrics.EditSharingButtonWidth;
        open.Enabled = false;
        _practices.SelectedIndexChanged += (_, _) =>
            open.Enabled = _practices.SelectedItems.Count > 0;
        open.Click += (_, _) => OpenSelectedPractice();
        _practices.DoubleClick += (_, _) => OpenSelectedPractice();
        selected.Click += (_, _) =>
        {
            if (_practices.SelectedItems.Count == 0)
            {
                AppMessageBox.Show("Seleziona una condivisione dall'elenco.", "Nessuna selezione", icon: MessageBoxIcon.Information, owner: this);
                return;
            }
            PracticeIdsToClose = new[] { _practices.SelectedItems[0].Tag?.ToString() ?? string.Empty };
            DialogResult = DialogResult.OK;
        };
        all.Click += (_, _) =>
        {
            PracticeIdsToClose = _items.Select(item => item.Id).ToList();
            DialogResult = DialogResult.OK;
        };
        later.DialogResult = DialogResult.Cancel;
        PlaceButtons(new[] { later, open, selected, all }, 405);
        CancelButton = later;
    }

    private void EnforcePracticesBackground()
    {
        if (_practices.IsDisposed) return;
        if (_practices.BackColor != Color.White)
            _practices.BackColor = Color.White;
        if (!_practices.IsHandleCreated) return;

        int white = ColorTranslator.ToWin32(Color.White);
        SendMessage(_practices.Handle, 0x1001, IntPtr.Zero, (IntPtr)white);
        SendMessage(_practices.Handle, 0x1026, IntPtr.Zero, (IntPtr)white);
        _practices.Invalidate();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr windowHandle,
        int message,
        IntPtr parameter,
        IntPtr value);

    private void OpenSelectedPractice()
    {
        if (_practices.SelectedItems.Count == 0) return;
        PracticeIdToOpen = _practices.SelectedItems[0].Tag?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(PracticeIdToOpen)) return;
        DialogResult = DialogResult.OK;
    }

    private void PlaceButtons(IReadOnlyList<MaterialButton> buttons, int y)
    {
        int width = buttons.Sum(button => button.Width) + (buttons.Count - 1) * UiMetrics.Spacing;
        int x = (ClientSize.Width - width) / 2;
        foreach (MaterialButton button in buttons)
        {
            button.Location = new Point(x, y);
            Controls.Add(button);
            x += button.Width + UiMetrics.Spacing;
        }
    }
}

public sealed class RoleSelectionForm : AppDialogForm
{
    private readonly InstitutionalMaterialComboBox _role = new();
    public string SelectedRole => _role.SelectedIndex == 1 ? "write" : "read";

    public RoleSelectionForm(string userName, string currentRole)
        : base("Modifica ruolo", new Size(420, 245))
    {
        Controls.Add(Caption(userName, new Point(24, 78), new Size(372, 24), true));
        _role.Hint = "Ruolo";
        _role.Bounds = new Rectangle(24, 115, 372, UiMetrics.MaterialInputHeight);
        _role.DropDownStyle = ComboBoxStyle.DropDownList;
        _role.Items.AddRange(new object[] { "Lettura", "Scrittura" });
        _role.SelectedIndex = currentRole.Equals("write", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        Controls.Add(_role);
        MaterialButton cancel = Button("Annulla", outlined: true);
        MaterialButton save = Button("Salva");
        cancel.Location = new Point(67, 188);
        save.Location = new Point(cancel.Right + UiMetrics.Spacing, 188);
        cancel.DialogResult = DialogResult.Cancel;
        save.DialogResult = DialogResult.OK;
        Controls.Add(cancel);
        Controls.Add(save);
        AcceptButton = save;
        CancelButton = cancel;
    }
}

public sealed class PressReviewRecipientForm : AppDialogForm
{
    private readonly AuthenticationService _authenticationService;
    private readonly string _internalDomain;
    private readonly InstitutionalMaterialTextBoxEdit _recipient = new();
    private readonly ListBox _suggestions = new();
    private readonly System.Windows.Forms.Panel _suggestionsHost = new();
    private readonly System.Windows.Forms.Timer _searchTimer = new() { Interval = 350 };
    private CancellationTokenSource? _searchCancellation;
    private bool _applyingSuggestion;
    public string Recipient { get; private set; } = string.Empty;

    public PressReviewRecipientForm(
        DateTime reviewDate,
        string defaultRecipient,
        AuthenticationService authenticationService,
        string internalDomain)
        : base("Reinvia collegamento", new Size(520, 410))
    {
        _authenticationService = authenticationService;
        _internalDomain = internalDomain;
        Controls.Add(Caption(
            $"Rassegna stampa del {reviewDate:dd/MM/yyyy}",
            new Point(24, 78),
            new Size(472, 25),
            true));
        Controls.Add(Caption(
            "Verifica o modifica il destinatario prima dell’invio.",
            new Point(24, 104),
            new Size(472, 24)));

        _recipient.Hint = "Indirizzo email del destinatario";
        _recipient.Text = defaultRecipient.Trim();
        _recipient.Bounds = new Rectangle(
            24,
            134,
            472,
            UiMetrics.MaterialInputHeight);
        Controls.Add(_recipient);

        _suggestionsHost.Bounds = new Rectangle(24, _recipient.Bottom, 472, 136);
        _suggestionsHost.Padding = new Padding(2);
        _suggestionsHost.BackColor = Color.White;
        _suggestionsHost.Visible = false;
        _suggestionsHost.Paint += (_, eventArgs) =>
        {
            using var pen = new Pen(AppThemeManager.PopupActiveLine, 2F);
            eventArgs.Graphics.DrawRectangle(
                pen,
                1,
                1,
                Math.Max(0, _suggestionsHost.ClientSize.Width - 3),
                Math.Max(0, _suggestionsHost.ClientSize.Height - 3));
        };
        _suggestions.Dock = DockStyle.Fill;
        _suggestions.BorderStyle = BorderStyle.None;
        _suggestions.BackColor = Color.White;
        _suggestions.Font = AppTypography.Create(10.5F);
        _suggestions.DrawMode = DrawMode.OwnerDrawFixed;
        _suggestions.IntegralHeight = false;
        _suggestions.ItemHeight = 28;
        _suggestions.DrawItem += DrawSuggestion;
        _suggestions.MouseClick += (_, _) => ApplySelectedSuggestion();
        _suggestions.MouseMove += (_, eventArgs) =>
        {
            int index = _suggestions.IndexFromPoint(eventArgs.Location);
            if (index >= 0 && index != _suggestions.SelectedIndex)
                _suggestions.SelectedIndex = index;
        };
        _suggestions.MouseLeave += (_, _) => _suggestions.SelectedIndex = -1;
        _suggestions.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Enter)
            {
                ApplySelectedSuggestion();
                eventArgs.Handled = true;
            }
            else if (eventArgs.KeyCode == Keys.Escape)
            {
                HideSuggestions();
                _recipient.Focus();
                eventArgs.Handled = true;
            }
        };
        _suggestionsHost.Controls.Add(_suggestions);
        Controls.Add(_suggestionsHost);
        _recipient.TextChanged += (_, _) => ScheduleSearch();
        _recipient.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Down && _suggestionsHost.Visible)
            {
                _suggestions.Focus();
                if (_suggestions.Items.Count > 0)
                    _suggestions.SelectedIndex = 0;
                eventArgs.Handled = true;
            }
            else if (eventArgs.KeyCode == Keys.Escape)
            {
                HideSuggestions();
            }
        };
        _searchTimer.Tick += async (_, _) => await SearchDirectoryAsync();

        MaterialButton cancel = Button("Annulla", outlined: true);
        MaterialButton send = Button("Invia");
        int groupWidth = cancel.Width + UiMetrics.Spacing + send.Width;
        cancel.Location = new Point((ClientSize.Width - groupWidth) / 2, 348);
        send.Location = new Point(cancel.Right + UiMetrics.Spacing, cancel.Top);
        cancel.DialogResult = DialogResult.Cancel;
        send.Click += (_, _) => ConfirmRecipient();
        Controls.Add(cancel);
        Controls.Add(send);
        AcceptButton = send;
        CancelButton = cancel;
        Shown += (_, _) =>
        {
            _recipient.Focus();
            _recipient.SelectAll();
        };
        FormClosed += (_, _) =>
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchTimer.Dispose();
        };
    }

    private void ScheduleSearch()
    {
        if (_applyingSuggestion) return;
        _searchTimer.Stop();
        string term = _recipient.Text.Trim();
        if (term.Length < 2
            || term.Contains('@')
            && !_internalDomain.Equals(
                term[(term.LastIndexOf('@') + 1)..],
                StringComparison.OrdinalIgnoreCase))
        {
            HideSuggestions();
            return;
        }
        _searchTimer.Start();
    }

    private async Task SearchDirectoryAsync()
    {
        _searchTimer.Stop();
        string term = _recipient.Text.Trim();
        if (term.Length < 2) return;

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        try
        {
            IReadOnlyList<AuthenticationService.DirectoryUserSuggestion> matches =
                await _authenticationService.SearchInternalUsersAsync(
                    term,
                    _internalDomain,
                    _searchCancellation.Token);
            if (IsDisposed
                || Disposing
                || !term.Equals(_recipient.Text.Trim(), StringComparison.OrdinalIgnoreCase))
                return;

            _suggestions.BeginUpdate();
            _suggestions.Items.Clear();
            foreach (AuthenticationService.DirectoryUserSuggestion match in matches)
                _suggestions.Items.Add(match);
            _suggestions.EndUpdate();
            _suggestionsHost.Height =
                Math.Min(5, Math.Max(1, matches.Count)) * _suggestions.ItemHeight
                + _suggestionsHost.Padding.Vertical;
            _suggestionsHost.Visible = matches.Count > 0;
            if (_suggestionsHost.Visible)
                _suggestionsHost.BringToFront();
        }
        catch (OperationCanceledException)
        {
            // Una nuova digitazione ha sostituito la ricerca precedente.
        }
        catch (HttpRequestException)
        {
            HideSuggestions();
        }
    }

    private void ApplySelectedSuggestion()
    {
        if (_suggestions.SelectedItem
            is not AuthenticationService.DirectoryUserSuggestion suggestion)
            return;

        _applyingSuggestion = true;
        _recipient.Text = suggestion.Email;
        _recipient.SelectionStart = _recipient.Text.Length;
        _applyingSuggestion = false;
        HideSuggestions();
        _recipient.Focus();
    }

    private void HideSuggestions()
    {
        _suggestionsHost.Visible = false;
        _suggestions.Items.Clear();
    }

    private void DrawSuggestion(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _suggestions.Items.Count) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color background = selected ? AppThemeManager.PopupActiveText : Color.White;
        Color foreground = selected ? Color.White : Color.FromArgb(45, 45, 45);
        using var backgroundBrush = new SolidBrush(background);
        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            _suggestions.Items[e.Index]?.ToString() ?? string.Empty,
            _suggestions.Font,
            new Rectangle(
                e.Bounds.X + 10,
                e.Bounds.Y,
                e.Bounds.Width - 18,
                e.Bounds.Height),
            foreground,
            TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.EndEllipsis);
    }

    private void ConfirmRecipient()
    {
        string candidate = _recipient.Text.Trim();
        if (!MailAddress.TryCreate(candidate, out MailAddress? address))
        {
            AppMessageBox.Show(
                "Inserisci un indirizzo email valido.",
                "Destinatario non valido",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning,
                this);
            _recipient.Focus();
            _recipient.SelectAll();
            return;
        }

        Recipient = address.Address;
        DialogResult = DialogResult.OK;
    }
}

public sealed class PracticeMetadataForm : AppDialogForm
{
    private readonly InstitutionalMaterialTextBoxEdit _practiceName = new();
    private readonly InstitutionalMaterialTextBoxEdit _requestedBy = new();
    private readonly InstitutionalMaterialMultiLineTextBoxEdit _notes = new();
    private readonly System.Windows.Forms.CheckBox _hasExpiration = new();
    private readonly DateTimePicker _expirationDate = new();
    public string PracticeName => _practiceName.Text.Trim();
    public string RequestedBy => _requestedBy.Text.Trim();
    public string Notes => _notes.Text.Trim();
    public DateTimeOffset? ExpirationDate => _hasExpiration.Checked ? _expirationDate.Value.Date : null;

    public PracticeMetadataForm(string? practiceName = null, SharePointService.FolderMetadata? metadata = null)
        : base(string.IsNullOrWhiteSpace(practiceName) ? "Dati della nuova condivisione" : "Modifica condivisione", new Size(520, 445))
    {
        bool editing = !string.IsNullOrWhiteSpace(practiceName);
        AddField("Nome condivisione", _practiceName, 82);
        _practiceName.Text = practiceName ?? string.Empty;
        AddField("Richiesto da", _requestedBy, 147);
        _requestedBy.Text = metadata?.RequestedBy ?? string.Empty;
        _notes.Hint = "Note";
        _notes.Bounds = new Rectangle(24, 212, 472, 109);
        _notes.Text = metadata?.Notes ?? string.Empty;
        Controls.Add(_notes);

        _hasExpiration.Text = "Imposta una scadenza";
        _hasExpiration.Bounds = new Rectangle(24, 340, 220, 28);
        DateTime existing = metadata?.ExpirationDate?.LocalDateTime.Date ?? DateTime.Today;
        _expirationDate.Bounds = new Rectangle(276, 340, 220, 28);
        _expirationDate.Format = DateTimePickerFormat.Short;
        _expirationDate.MinDate = existing < DateTime.Today ? existing : DateTime.Today;
        _expirationDate.Value = metadata?.ExpirationDate?.LocalDateTime.Date ?? DateTime.Today.AddMonths(1);
        _expirationDate.Enabled = metadata?.ExpirationDate is not null;
        _hasExpiration.Checked = metadata?.ExpirationDate is not null;
        _hasExpiration.CheckedChanged += (_, _) => _expirationDate.Enabled = _hasExpiration.Checked;
        Controls.Add(_hasExpiration);
        Controls.Add(_expirationDate);

        MaterialButton cancel = Button("Annulla", outlined: true);
        MaterialButton save = Button(editing ? "Salva" : "Crea\ncondivisione");
        cancel.Location = new Point(117, 388);
        save.Location = new Point(cancel.Right + UiMetrics.Spacing, 388);
        cancel.DialogResult = DialogResult.Cancel;
        save.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(PracticeName))
            {
                AppMessageBox.Show("Inserisci il nome della condivisione.", "Dato obbligatorio", icon: MessageBoxIcon.Warning, owner: this);
                return;
            }
            if (string.IsNullOrWhiteSpace(RequestedBy))
            {
                AppMessageBox.Show("Indica chi ha richiesto la condivisione.", "Dato obbligatorio", icon: MessageBoxIcon.Warning, owner: this);
                return;
            }
            DialogResult = DialogResult.OK;
        };
        Controls.Add(cancel);
        Controls.Add(save);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void AddField(string label, MaterialTextBoxEdit field, int y)
    {
        field.Hint = label;
        field.UseTallSize = true;
        field.Bounds = new Rectangle(24, y, 472, UiMetrics.MaterialInputHeight);
        Controls.Add(field);
    }
}

public sealed record InviteRecipient(string Email, string Role);

public sealed class SendPracticeLinkForm : AppDialogForm
{
    private readonly DataGridView _recipients = new();
    public IReadOnlyList<string> SelectedEmails => _recipients.Rows.Cast<DataGridViewRow>()
        .Where(row => Convert.ToBoolean(row.Cells[0].Value ?? false))
        .Select(row => row.Cells[1].Value?.ToString() ?? string.Empty)
        .Where(email => !string.IsNullOrWhiteSpace(email))
        .ToList();

    public SendPracticeLinkForm(string practiceName, IReadOnlyList<string> authorizedEmails)
        : base("Invia link alla condivisione", new Size(620, 430))
    {
        Controls.Add(Caption("Condivisione", new Point(24, 78), new Size(572, 20), true));
        Controls.Add(Caption(practiceName, new Point(24, 100), new Size(572, 24)));
        Controls.Add(Caption("Seleziona i destinatari ai quali inviare nuovamente il collegamento", new Point(24, 135), new Size(572, 24)));
        _recipients.Bounds = new Rectangle(24, 165, 572, 180);
        _recipients.AllowUserToAddRows = false;
        _recipients.AllowUserToDeleteRows = false;
        _recipients.AllowUserToResizeRows = false;
        _recipients.RowHeadersVisible = false;
        _recipients.MultiSelect = false;
        _recipients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _recipients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _recipients.BackgroundColor = Color.White;
        _recipients.BorderStyle = BorderStyle.FixedSingle;
        _recipients.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _recipients.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _recipients.ColumnHeadersHeight = 34;
        _recipients.RowTemplate.Height = 32;
        _recipients.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Invia",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
            MinimumWidth = 54
        });
        _recipients.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Email autorizzata",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        InstitutionalSelectionStyle.Apply(_recipients);
        foreach (string email in authorizedEmails) _recipients.Rows.Add(false, email);
        _recipients.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_recipients.IsCurrentCellDirty)
                _recipients.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        Controls.Add(_recipients);

        MaterialButton cancel = Button("Annulla", outlined: true);
        MaterialButton send = Button("Invia");
        int groupWidth = cancel.Width + UiMetrics.Spacing + send.Width;
        cancel.Location = new Point((ClientSize.Width - groupWidth) / 2, 365);
        send.Location = new Point(cancel.Right + UiMetrics.Spacing, cancel.Top);
        cancel.DialogResult = DialogResult.Cancel;
        send.Click += (_, _) =>
        {
            if (SelectedEmails.Count == 0)
            {
                AppMessageBox.Show("Seleziona almeno un destinatario.", "Nessun destinatario", icon: MessageBoxIcon.Warning, owner: this);
                return;
            }
            DialogResult = DialogResult.OK;
        };
        Controls.Add(cancel);
        Controls.Add(send);
        AcceptButton = send;
        CancelButton = cancel;
    }
}

public sealed class InviteModalForm : AppDialogForm
{
    private readonly AuthenticationService _authenticationService;
    private readonly string _internalDomain;
    private readonly string _sharingName;
    private readonly InstitutionalMaterialMultiLineTextBoxEdit _emailInput = new();
    private readonly ListBox _directorySuggestions = new();
    private readonly InstitutionalBorderPanel _directorySuggestionsHost = new();
    private readonly System.Windows.Forms.Timer _directorySearchTimer = new() { Interval = 350 };
    private CancellationTokenSource? _directorySearchCancellation;
    private bool _applyingDirectorySuggestion;
    private readonly InstitutionalMaterialComboBox _defaultRole = new();
    private readonly DataGridView _recipients = new();
    private readonly InstitutionalMaterialMultiLineTextBoxEdit _message = new();
    private bool _invitationsConfirmed;
    public IReadOnlyList<InviteRecipient> Invitations => _recipients.Rows.Cast<DataGridViewRow>()
        .Where(row => !row.IsNewRow)
        .Select(row => new InviteRecipient(row.Cells[0].Value?.ToString() ?? string.Empty, row.Cells[1].Value?.ToString() ?? "Lettura"))
        .ToList();
    public string Message => _message.Text.Trim();

    public InviteModalForm(
        string practiceName,
        AuthenticationService authenticationService,
        string internalDomain)
        : base("Invita utenti alla Data Room", new Size(780, 650))
    {
        _authenticationService = authenticationService;
        _internalDomain = internalDomain;
        _sharingName = practiceName;
        AddReadOnlyField("Condivisione", practiceName, 78);
        _emailInput.Hint = "Email da aggiungere (una per riga oppure separate da virgola o punto e virgola)";
        _emailInput.Bounds = new Rectangle(24, 145, 500, 87);
        Controls.Add(_emailInput);
        _directorySuggestionsHost.Bounds = new Rectangle(24, 230, 500, 136);
        _directorySuggestionsHost.Padding = new Padding(2);
        _directorySuggestionsHost.BackColor = Color.White;
        _directorySuggestionsHost.Visible = false;
        _directorySuggestions.Dock = DockStyle.Fill;
        _directorySuggestions.BorderStyle = BorderStyle.None;
        _directorySuggestions.BackColor = Color.White;
        _directorySuggestions.Font = AppTypography.Create(10.5F);
        _directorySuggestions.DrawMode = DrawMode.OwnerDrawFixed;
        _directorySuggestions.IntegralHeight = false;
        _directorySuggestions.ItemHeight = 28;
        _directorySuggestions.DrawItem += DrawDirectorySuggestion;
        _directorySuggestions.MouseClick += (_, _) => ApplySelectedDirectorySuggestion();
        _directorySuggestions.MouseMove += (_, eventArgs) =>
        {
            int index = _directorySuggestions.IndexFromPoint(eventArgs.Location);
            if (index >= 0 && index != _directorySuggestions.SelectedIndex)
                _directorySuggestions.SelectedIndex = index;
        };
        _directorySuggestions.MouseLeave += (_, _) => _directorySuggestions.SelectedIndex = -1;
        _directorySuggestions.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Enter)
            {
                ApplySelectedDirectorySuggestion();
                eventArgs.Handled = true;
            }
            else if (eventArgs.KeyCode == Keys.Escape)
            {
                HideDirectorySuggestions();
                _emailInput.Focus();
                eventArgs.Handled = true;
            }
        };
        _directorySuggestionsHost.Controls.Add(_directorySuggestions);
        Controls.Add(_directorySuggestionsHost);
        _emailInput.TextChanged += (_, _) => ScheduleDirectorySearch();
        _emailInput.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Down && _directorySuggestionsHost.Visible)
            {
                _directorySuggestions.Focus();
                if (_directorySuggestions.Items.Count > 0) _directorySuggestions.SelectedIndex = 0;
                eventArgs.Handled = true;
            }
            else if (eventArgs.KeyCode == Keys.Escape)
            {
                HideDirectorySuggestions();
            }
        };
        _directorySearchTimer.Tick += async (_, _) => await SearchDirectoryAsync();
        _defaultRole.Hint = "Ruolo predefinito";
        _defaultRole.Bounds = new Rectangle(542, 145, 214, UiMetrics.MaterialInputHeight);
        _defaultRole.DropDownStyle = ComboBoxStyle.DropDownList;
        _defaultRole.Items.AddRange(new object[] { "Lettura", "Scrittura" });
        _defaultRole.SelectedIndex = 0;
        Controls.Add(_defaultRole);
        MaterialButton add = Button("Aggiungi", outlined: true);
        add.Location = new Point(621, 202);
        add.Click += (_, _) => AddRecipients();
        Controls.Add(add);

        _recipients.Bounds = new Rectangle(24, 259, 732, 205);
        _recipients.AllowUserToAddRows = false;
        _recipients.AllowUserToDeleteRows = true;
        _recipients.AllowUserToResizeRows = false;
        _recipients.ColumnHeadersHeight = 34;
        _recipients.RowTemplate.Height = 32;
        _recipients.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _recipients.BackgroundColor = Color.White;
        _recipients.BorderStyle = BorderStyle.FixedSingle;
        _recipients.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _recipients.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        _recipients.MultiSelect = true;
        _recipients.RowHeadersVisible = false;
        _recipients.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _recipients.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Email",
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _recipients.Columns.Add(new DataGridViewComboBoxColumn
        {
            HeaderText = "Ruolo",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
            MinimumWidth = 105,
            DataSource = new[] { "Lettura", "Scrittura" },
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox
        });
        InstitutionalSelectionStyle.Apply(_recipients);
        Controls.Add(_recipients);

        _message.Hint = "Messaggio email comune (facoltativo, massimo 2.000 caratteri)";
        _message.Bounds = new Rectangle(24, 488, 732, 86);
        _message.MaxLength = 2000;
        Controls.Add(_message);

        MaterialButton remove = Button("Rimuovi\ndall'elenco", outlined: true);
        MaterialButton cancel = Button("Annulla", outlined: true);
        MaterialButton send = Button("Invia inviti");
        remove.Enabled = false;
        _recipients.SelectionChanged += (_, _) =>
            remove.Enabled = _recipients.SelectedRows.Count > 0;
        remove.Click += (_, _) => RemoveSelectedRows();
        cancel.DialogResult = DialogResult.Cancel;
        send.Click += (_, _) => ConfirmInvitations();
        int x = (ClientSize.Width - (UiMetrics.ButtonWidth * 3 + UiMetrics.Spacing * 2)) / 2;
        foreach (MaterialButton button in new[] { remove, cancel, send })
        {
            button.Location = new Point(x, 592);
            Controls.Add(button);
            x += UiMetrics.ButtonWidth + UiMetrics.Spacing;
        }
        AcceptButton = send;
        CancelButton = cancel;
        FormClosing += ConfirmDiscardInvitations;
        FormClosed += (_, _) =>
        {
            _directorySearchCancellation?.Cancel();
            _directorySearchCancellation?.Dispose();
            _directorySearchTimer.Dispose();
        };
    }

    private void ScheduleDirectorySearch()
    {
        if (_applyingDirectorySuggestion) return;
        _directorySearchTimer.Stop();
        string term = CurrentEmailFragment().Term;
        if (term.Length < 2 || term.Contains('@') && !term.EndsWith(_internalDomain, StringComparison.OrdinalIgnoreCase))
        {
            HideDirectorySuggestions();
            return;
        }
        _directorySearchTimer.Start();
    }

    private async Task SearchDirectoryAsync()
    {
        _directorySearchTimer.Stop();
        string term = CurrentEmailFragment().Term;
        if (term.Length < 2) return;

        _directorySearchCancellation?.Cancel();
        _directorySearchCancellation?.Dispose();
        _directorySearchCancellation = new CancellationTokenSource();
        try
        {
            IReadOnlyList<AuthenticationService.DirectoryUserSuggestion> matches =
                await _authenticationService.SearchInternalUsersAsync(
                    term,
                    _internalDomain,
                    _directorySearchCancellation.Token);
            if (IsDisposed || Disposing || !term.Equals(CurrentEmailFragment().Term, StringComparison.OrdinalIgnoreCase)) return;

            _directorySuggestions.BeginUpdate();
            _directorySuggestions.Items.Clear();
            foreach (AuthenticationService.DirectoryUserSuggestion match in matches)
                _directorySuggestions.Items.Add(match);
            _directorySuggestions.EndUpdate();
            _directorySuggestionsHost.Height =
                Math.Min(5, Math.Max(1, matches.Count)) * _directorySuggestions.ItemHeight
                + _directorySuggestionsHost.Padding.Vertical;
            _directorySuggestionsHost.Visible = matches.Count > 0;
            if (_directorySuggestionsHost.Visible) _directorySuggestionsHost.BringToFront();
        }
        catch (OperationCanceledException)
        {
            // Una nuova digitazione ha sostituito la ricerca in corso.
        }
        catch (HttpRequestException)
        {
            HideDirectorySuggestions();
        }
    }

    private void ApplySelectedDirectorySuggestion()
    {
        if (_directorySuggestions.SelectedItem is not AuthenticationService.DirectoryUserSuggestion suggestion) return;
        (int start, int end, _) = CurrentEmailFragment();
        string current = _emailInput.Text;
        string separator = end == current.Length ? Environment.NewLine : string.Empty;
        _applyingDirectorySuggestion = true;
        _emailInput.Text = current[..start] + suggestion.Email + separator + current[end..];
        _emailInput.SelectionStart = start + suggestion.Email.Length + separator.Length;
        _applyingDirectorySuggestion = false;
        HideDirectorySuggestions();
        _emailInput.Focus();
    }

    private (int Start, int End, string Term) CurrentEmailFragment()
    {
        string text = _emailInput.Text;
        int caret = Math.Clamp(_emailInput.SelectionStart, 0, text.Length);
        int start = caret;
        while (start > 0 && !IsEmailSeparator(text[start - 1])) start--;
        while (start < caret && char.IsWhiteSpace(text[start])) start++;
        int end = caret;
        while (end < text.Length && !IsEmailSeparator(text[end])) end++;
        return (start, end, text[start..caret].Trim());
    }

    private static bool IsEmailSeparator(char value) => value is '\r' or '\n' or ',' or ';';

    private void HideDirectorySuggestions()
    {
        _directorySuggestionsHost.Visible = false;
        _directorySuggestions.Items.Clear();
    }

    private void DrawDirectorySuggestion(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _directorySuggestions.Items.Count) return;

        bool selected = (e.State & DrawItemState.Selected) != 0;
        Color background = selected ? AppThemeManager.PopupActiveText : Color.White;
        Color foreground = selected ? Color.White : Color.FromArgb(45, 45, 45);
        using var backgroundBrush = new SolidBrush(background);
        e.Graphics.FillRectangle(backgroundBrush, e.Bounds);
        TextRenderer.DrawText(
            e.Graphics,
            _directorySuggestions.Items[e.Index]?.ToString() ?? string.Empty,
            _directorySuggestions.Font,
            new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 18, e.Bounds.Height),
            foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private sealed class InstitutionalBorderPanel : System.Windows.Forms.Panel
    {
        public InstitutionalBorderPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(AppThemeManager.PopupActiveLine, 2F);
            e.Graphics.DrawRectangle(
                pen,
                1,
                1,
                Math.Max(0, ClientSize.Width - 3),
                Math.Max(0, ClientSize.Height - 3));
        }
    }

    private void AddReadOnlyField(string label, string value, int y)
    {
        Controls.Add(new InstitutionalMaterialTextBoxEdit
        {
            Hint = label,
            Text = value,
            Bounds = new Rectangle(24, y, 732, UiMetrics.MaterialInputHeight),
            ReadOnly = true,
            UseTallSize = true
        });
    }

    private void AddRecipients()
    {
        string[] candidates = _emailInput.Text.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var invalid = new List<string>();
        HashSet<string> existing = Invitations.Select(item => item.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string candidate in candidates)
        {
            if (!MailAddress.TryCreate(candidate, out MailAddress? address))
            {
                invalid.Add(candidate);
                continue;
            }
            if (existing.Add(address.Address))
                _recipients.Rows.Add(address.Address, _defaultRole.SelectedItem?.ToString() ?? "Lettura");
        }
        _emailInput.Clear();
        if (invalid.Count > 0)
            AppMessageBox.Show($"Indirizzi non validi ignorati:\n{string.Join("\n", invalid)}", "Verifica indirizzi", icon: MessageBoxIcon.Warning, owner: this);
    }

    private void RemoveSelectedRows()
    {
        foreach (DataGridViewRow row in _recipients.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(row => row.Index))
            _recipients.Rows.RemoveAt(row.Index);
    }

    private void ConfirmInvitations()
    {
        AddRecipients();
        _recipients.EndEdit();
        if (Invitations.Count == 0)
        {
            AppMessageBox.Show("Aggiungi almeno un indirizzo email valido.", "Nessun destinatario", icon: MessageBoxIcon.Warning, owner: this);
            return;
        }

        string recipients = string.Join(
            "\n",
            Invitations.Select(invitation => $"• {invitation.Email} — {invitation.Role}"));
        DialogResult confirmation = AppMessageBox.Show(
            $"Confermi l’assegnazione degli accessi e l’invio del collegamento?\n\n"
            + $"Condivisione: {_sharingName}\n\n"
            + $"Destinatari:\n{recipients}",
            "Conferma invio inviti",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            this);
        if (confirmation != DialogResult.Yes)
            return;

        _invitationsConfirmed = true;
        DialogResult = DialogResult.OK;
    }

    private void ConfirmDiscardInvitations(object? sender, FormClosingEventArgs e)
    {
        if (_invitationsConfirmed || !HasPendingInvitationData()) return;

        DialogResult confirmation = AppMessageBox.Show(
            "Sono presenti destinatari o informazioni non ancora inviate.\n\nConfermi di voler chiudere la finestra e annullare l'inserimento?",
            "Conferma annullamento",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            this);
        if (confirmation != DialogResult.Yes)
        {
            e.Cancel = true;
            DialogResult = DialogResult.None;
        }
    }

    private bool HasPendingInvitationData() =>
        !string.IsNullOrWhiteSpace(_emailInput.Text)
        || _recipients.Rows.Cast<DataGridViewRow>().Any(row => !row.IsNewRow)
        || !string.IsNullOrWhiteSpace(_message.Text);
}

public sealed class AuditViewerForm : AppDialogForm
{
    private readonly SharePointService _service;
    private readonly InstitutionalMaterialTextBoxEdit _search = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly ComboBox _scope = new();
    private readonly ListView _activitiesList = new();
    private readonly ListView _acceptancesList = new();
    private readonly ListView _sharePointList = new();
    private readonly Label _sharePointStatus = new();
    private readonly AuditLoadingIndicator _sharePointProgress = new();
    private readonly TabControl _tabs = new();
    private readonly CancellationTokenSource _loadingCancellation = new();
    private bool _sharePointLoaded;
    private bool _sharePointLoading;
    private bool _closing;
    private readonly string? _selectedSharingName;
    private readonly string? _selectedSharingId;
    private readonly Dictionary<string, string> _sharingScopeIds = new(StringComparer.OrdinalIgnoreCase);
    private const string CollectionScopeLabel = "Intera raccolta";
    private const string CurrentSharingScopeLabel = "Condivisione attuale";
    private IReadOnlyList<SharePointService.AuditActivity> _activities = Array.Empty<SharePointService.AuditActivity>();
    private IReadOnlyList<SharePointService.DisclaimerAcceptance> _acceptances = Array.Empty<SharePointService.DisclaimerAcceptance>();
    private IReadOnlyList<SharePointService.RecentSharePointActivity> _sharePointActivities = Array.Empty<SharePointService.RecentSharePointActivity>();

    public AuditViewerForm(
        SharePointService service,
        string? selectedSharingName = null,
        string? selectedSharingId = null)
        : base("Registro audit", new Size(1120, 636))
    {
        _service = service;
        _selectedSharingName = selectedSharingName;
        _selectedSharingId = selectedSharingId;
        _search.Hint = "Cerca utente, email, condivisione, operazione o dettaglio";
        _search.UseTallSize = true;
        _search.Bounds = new Rectangle(20, 78, 430, UiMetrics.MaterialInputHeight);
        Controls.Add(_search);
        ConfigureDate(_from, "Dal", 475);
        ConfigureDate(_to, "Al", 690);
        Controls.Add(Caption("Ambito", new Point(905, 70), new Size(195, 20)));
        _scope.Bounds = new Rectangle(905, 92, 195, 28);
        _scope.DropDownStyle = ComboBoxStyle.DropDownList;
        if (!string.IsNullOrWhiteSpace(_selectedSharingName)
            && !string.IsNullOrWhiteSpace(_selectedSharingId))
        {
            _scope.Items.Add(CurrentSharingScopeLabel);
            _sharingScopeIds[CurrentSharingScopeLabel] = _selectedSharingId;
        }
        _scope.Items.Add(CollectionScopeLabel);
        _scope.SelectedIndex = 0;
        _scope.SelectedIndexChanged += async (_, _) =>
        {
            if (_sharePointLoaded) await LoadSharePointDataAsync(force: true);
            else ApplyFilters();
        };
        Controls.Add(_scope);
        ConfigureList(_activitiesList, new[]
        {
            ("Data e ora",145),("Utente",175),("Email",210),("Operazione",145),("Condivisione",150),("Destinatario",210),("Esito",80),("Dettagli",420)
        });
        ConfigureList(_acceptancesList, new[]
        {
            ("Data e ora",145),("Utente",190),("Email",230),("Disclaimer",100),("Versione app",110),("Esito",90),("Hash testo",300)
        });
        var activityPage = new System.Windows.Forms.TabPage("Attività") { Padding = new Padding(8) };
        var acceptancePage = new System.Windows.Forms.TabPage("Prese visione") { Padding = new Padding(8) };
        ConfigureList(_sharePointList, new[]
        {
            ("Data e ora",145),("Utente",230),("Attività",150),("Elemento",250),("Tipo",90),("Dettagli",330),("Percorso",430)
        });
        var sharePointPage = new System.Windows.Forms.TabPage("Attività SharePoint") { Padding = new Padding(8) };
        _activitiesList.Dock = DockStyle.Fill;
        _acceptancesList.Dock = DockStyle.Fill;
        _sharePointList.Dock = DockStyle.Fill;
        activityPage.Controls.Add(_activitiesList);
        acceptancePage.Controls.Add(_acceptancesList);
        var sharePointLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        sharePointLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        sharePointLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _sharePointStatus.Dock = DockStyle.Fill;
        _sharePointStatus.Text = "Apri questa scheda per caricare le attività recenti direttamente da SharePoint.";
        _sharePointStatus.TextAlign = ContentAlignment.MiddleLeft;
        _sharePointStatus.Padding = new Padding(6, 0, 0, 8);
        _sharePointStatus.Font = AppTypography.Create(10F, FontStyle.Bold);
        _sharePointStatus.ForeColor = Color.FromArgb(62, 88, 133);
        var progressPanel = new System.Windows.Forms.Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        progressPanel.Controls.Add(_sharePointStatus);
        _sharePointProgress.Dock = DockStyle.Bottom;
        _sharePointProgress.Height = 7;
        _sharePointProgress.Visible = false;
        progressPanel.Controls.Add(_sharePointProgress);
        sharePointLayout.Controls.Add(progressPanel, 0, 0);
        sharePointLayout.Controls.Add(_sharePointList, 0, 1);
        sharePointPage.Controls.Add(sharePointLayout);
        _tabs.Bounds = new Rectangle(20, 135, 1080, 425);
        _tabs.TabPages.Add(activityPage);
        _tabs.TabPages.Add(acceptancePage);
        _tabs.TabPages.Add(sharePointPage);
        Controls.Add(_tabs);

        MaterialButton filter = Button("Applica filtri");
        MaterialButton refresh = Button("Aggiorna");
        MaterialButton export = Button("Esporta CSV");
        MaterialButton close = Button("Chiudi", outlined: true);
        filter.Click += async (_, _) =>
        {
            if (_tabs.SelectedIndex == 2) await LoadSharePointDataAsync(force: true);
            else ApplyFilters();
        };
        refresh.Click += async (_, _) =>
        {
            if (_tabs.SelectedIndex == 2) await LoadSharePointDataAsync(force: true);
            else await LoadDataAsync();
        };
        export.Click += (_, _) => ExportVisibleRows();
        close.DialogResult = DialogResult.Cancel;
        int x = (ClientSize.Width - (UiMetrics.ButtonWidth * 4 + UiMetrics.Spacing * 3)) / 2;
        foreach (MaterialButton button in new[] { filter, refresh, export, close })
        {
            button.Location = new Point(x, 576);
            Controls.Add(button);
            x += UiMetrics.ButtonWidth + UiMetrics.Spacing;
        }
        AcceptButton = filter;
        CancelButton = close;
        _tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (_tabs.SelectedIndex == 2) await LoadSharePointDataAsync();
        };
        Shown += async (_, _) => await LoadDataAsync();
        FormClosing += (_, _) =>
        {
            _closing = true;
            _loadingCancellation.Cancel();
        };
    }

    private void ConfigureDate(DateTimePicker picker, string label, int x)
    {
        Controls.Add(Caption(label, new Point(x, 70), new Size(195, 20)));
        picker.Bounds = new Rectangle(x, 92, 195, 28);
        picker.Format = DateTimePickerFormat.Short;
        picker.ShowCheckBox = true;
        picker.Checked = false;
        Controls.Add(picker);
    }

    private static void ConfigureList(ListView list, IEnumerable<(string Name, int Width)> columns)
    {
        list.View = View.Details;
        list.FullRowSelect = true;
        list.HideSelection = false;
        foreach ((string name, int width) in columns) list.Columns.Add(name, width);
        InstitutionalSelectionStyle.Apply(list);
    }

    private async Task LoadDataAsync()
    {
        UseWaitCursor = true;
        try
        {
            Task<IReadOnlyList<SharePointService.AuditActivity>> activities = _service.GetAuditActivitiesAsync();
            Task<IReadOnlyList<SharePointService.DisclaimerAcceptance>> acceptances = _service.GetDisclaimerAcceptancesAsync();
            await Task.WhenAll(activities, acceptances);
            _activities = await activities;
            _acceptances = await acceptances;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(ex.Message, "Lettura registro non riuscita", icon: MessageBoxIcon.Error, owner: this);
        }
        finally { UseWaitCursor = false; }
    }

    private async Task LoadSharePointDataAsync(bool force = false)
    {
        if (_sharePointLoading || (_sharePointLoaded && !force)) return;
        DateTimeOffset now = DateTimeOffset.Now;
        DateTimeOffset requestedFrom = _from.Checked
            ? new DateTimeOffset(_from.Value.Date, TimeZoneInfo.Local.GetUtcOffset(_from.Value.Date))
            : now.AddDays(-7);
        DateTimeOffset requestedTo = _to.Checked
            ? new DateTimeOffset(_to.Value.Date.AddDays(1).AddTicks(-1), TimeZoneInfo.Local.GetUtcOffset(_to.Value.Date))
            : now;
        if (requestedFrom > requestedTo)
        {
            AppMessageBox.Show(
                "La data iniziale non può essere successiva alla data finale.",
                "Intervallo non valido",
                icon: MessageBoxIcon.Warning,
                owner: this);
            return;
        }

        _sharePointLoading = true;
        _sharePointStatus.Text = IsSelectedSharingScope
            ? $"Lettura delle attività recenti di '{SelectedSharingScopeName}' da SharePoint…"
            : "Lettura delle attività recenti della raccolta da SharePoint…";
        _sharePointProgress.Visible = true;
        _sharePointProgress.Start();
        _sharePointList.Enabled = false;
        try
        {
            string? itemId = SelectedSharingScopeId;
            _sharePointActivities = await _service.GetRecentActivitiesAsync(
                itemId,
                _loadingCancellation.Token);
            _sharePointLoaded = true;
            int visibleCount = _sharePointActivities.Count;
            _sharePointStatus.Text = IsSelectedSharingScope
                ? $"Attività caricate per '{SelectedSharingScopeName}': {visibleCount}."
                : $"Attività caricate per la raccolta documenti: {visibleCount}.";
            ApplyFilters();
        }
        catch (OperationCanceledException) when (_closing || _loadingCancellation.IsCancellationRequested)
        {
            // Chiusura della finestra durante la lettura SharePoint.
        }
        catch (Exception ex)
        {
            _sharePointActivities = Array.Empty<SharePointService.RecentSharePointActivity>();
            _sharePointStatus.Text = "Attività SharePoint non disponibili. Premi Aggiorna per riprovare.";
            AppMessageBox.Show(
                ex.Message,
                "Attività SharePoint non disponibili",
                icon: MessageBoxIcon.Warning,
                owner: this);
        }
        finally
        {
            _sharePointProgress.Stop();
            _sharePointProgress.Visible = false;
            _sharePointLoading = false;
            if (!IsDisposed) _sharePointList.Enabled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _loadingCancellation.Cancel();
            _loadingCancellation.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ApplyFilters()
    {
        string search = _search.Text.Trim();
        DateTimeOffset? from = _from.Checked ? new DateTimeOffset(_from.Value.Date, TimeZoneInfo.Local.GetUtcOffset(_from.Value.Date)) : null;
        DateTimeOffset? to = _to.Checked ? new DateTimeOffset(_to.Value.Date.AddDays(1).AddTicks(-1), TimeZoneInfo.Local.GetUtcOffset(_to.Value.Date)) : null;
        _activitiesList.Items.Clear();
        foreach (SharePointService.AuditActivity entry in _activities.Where(entry =>
                     (!IsSelectedSharingScope || ScopeMatches(entry.PracticeName))
                     && InRange(entry.Timestamp, from, to)
                     && Contains(search, entry.UserName, entry.Email, entry.Operation, entry.PracticeName, entry.Outcome, entry.Recipient, entry.Details)))
        {
            var item = new ListViewItem(Format(entry.Timestamp));
            item.SubItems.AddRange(new[] { entry.UserName, entry.Email, entry.Operation, entry.PracticeName, entry.Recipient, entry.Outcome, entry.Details });
            _activitiesList.Items.Add(item);
        }
        _acceptancesList.Items.Clear();
        foreach (SharePointService.DisclaimerAcceptance entry in _acceptances.Where(entry => InRange(entry.Timestamp, from, to) && Contains(search, entry.UserName, entry.Email, entry.DisclaimerVersion, entry.ApplicationVersion, entry.Outcome, entry.TextHash)))
        {
            var item = new ListViewItem(Format(entry.Timestamp));
            item.SubItems.AddRange(new[] { entry.UserName, entry.Email, entry.DisclaimerVersion, entry.ApplicationVersion.Split('+')[0], entry.Outcome, entry.TextHash });
            _acceptancesList.Items.Add(item);
        }
        _sharePointList.Items.Clear();
        foreach (SharePointService.RecentSharePointActivity entry in _sharePointActivities.Where(entry =>
                     InRange(entry.Timestamp, from, to)
                     && Contains(search, entry.User, entry.Operation, entry.ItemName, entry.ItemType, entry.Details, entry.Path)))
        {
            var item = new ListViewItem(Format(entry.Timestamp));
            item.SubItems.AddRange(new[]
            {
                entry.User, entry.Operation, entry.ItemName, entry.ItemType,
                entry.Details, entry.Path
            });
            _sharePointList.Items.Add(item);
        }
        if (_sharePointLoaded && !_sharePointLoading)
            _sharePointStatus.Text = IsSelectedSharingScope
                ? $"Attività caricate per '{SelectedSharingScopeName}': {_sharePointList.Items.Count}."
                : $"Attività caricate per la raccolta documenti: {_sharePointList.Items.Count}.";
    }

    private void ExportVisibleRows()
    {
        ListView list = _tabs.SelectedIndex switch
        {
            0 => _activitiesList,
            1 => _acceptancesList,
            _ => _sharePointList
        };
        if (list.Items.Count == 0)
        {
            AppMessageBox.Show("Non ci sono righe da esportare.", "Esportazione", icon: MessageBoxIcon.Information, owner: this);
            return;
        }
        string fileName = _tabs.SelectedIndex switch
        {
            0 => "RegistroAudit.csv",
            1 => "PreseVisione.csv",
            _ => "AttivitaSharePoint.csv"
        };
        using var dialog = new SaveFileDialog { Filter = "File CSV (*.csv)|*.csv", FileName = fileName, AddExtension = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var csv = new System.Text.StringBuilder();
        csv.AppendLine(string.Join(";", list.Columns.Cast<ColumnHeader>().Select(column => Csv(column.Text))));
        foreach (ListViewItem item in list.Items)
            csv.AppendLine(string.Join(";", item.SubItems.Cast<ListViewItem.ListViewSubItem>().Select(value => Csv(value.Text))));
        File.WriteAllText(dialog.FileName, csv.ToString(), new System.Text.UTF8Encoding(true));
        AppMessageBox.Show("Esportazione completata.", "Esportazione", icon: MessageBoxIcon.Information, owner: this);
    }

    private static bool InRange(DateTimeOffset value, DateTimeOffset? from, DateTimeOffset? to) => (!from.HasValue || value >= from) && (!to.HasValue || value <= to);
    private bool IsSelectedSharingScope =>
        SelectedSharingScopeId is not null;
    private string? SelectedSharingScopeId =>
        _scope.SelectedItem is string label
        && _sharingScopeIds.TryGetValue(label, out string? itemId)
            ? itemId
            : null;
    private string SelectedSharingScopeName =>
        IsSelectedSharingScope
            ? _selectedSharingName ?? "Condivisione attuale"
            : CollectionScopeLabel;
    private bool ScopeMatches(string sharingName) =>
        sharingName.Equals(SelectedSharingScopeName, StringComparison.OrdinalIgnoreCase);
    private static bool Contains(string search, params string[] values) => string.IsNullOrWhiteSpace(search) || values.Any(value => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);
    private static string Format(DateTimeOffset value) => value == DateTimeOffset.MinValue ? "-" : value.LocalDateTime.ToString("dd/MM/yyyy HH:mm:ss");
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private sealed class AuditLoadingIndicator : Control
    {
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 28 };
        private int _position;

        public AuditLoadingIndicator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            _timer.Tick += (_, _) =>
            {
                _position = (_position + 7) % Math.Max(1, Width + 150);
                Invalidate();
            };
        }

        public void Start()
        {
            _position = 0;
            _timer.Start();
        }

        public void Stop() => _timer.Stop();

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.FromArgb(214, 226, 238));
            int left = _position - 150;
            using var fill = new SolidBrush(Color.FromArgb(62, 88, 133));
            e.Graphics.FillRectangle(fill, left, 0, 150, Height);
            using var accent = new SolidBrush(Color.FromArgb(201, 183, 106));
            e.Graphics.FillRectangle(accent, left, 0, 150, Math.Min(2, Height));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _timer.Dispose();
            base.Dispose(disposing);
        }
    }
}
