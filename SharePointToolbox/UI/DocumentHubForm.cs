using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharePointToolbox.Configuration;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.UI.Theming;

namespace SharePointToolbox.UI;

public sealed class DocumentHubForm : InstitutionalMaterialForm
{
    private readonly IServiceProvider _services;
    private readonly BrandingOptions _branding;
    private MainForm? _preparedDataRoom;
    private bool _exitConfirmed;

    public DocumentHubForm(
        IServiceProvider services,
        IOptions<BrandingOptions> branding,
        AppThemeManager theme)
    {
        _services = services;
        _branding = branding.Value;
        Text = $"{_branding.StudioName} Document Hub";
        ClientSize = new Size(760, 340);
        StartPosition = FormStartPosition.CenterScreen;
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = true;
        theme.ApplyTo(this);
        BuildLayout(theme.Palette);
        Activated += (_, _) => ResetEnvironmentCards(this);
        FormClosing += ConfirmExit;
        FormClosed += (_, _) =>
        {
            _preparedDataRoom?.Dispose();
            _preparedDataRoom = null;
        };
    }

    internal void SetPreparedDataRoom(MainForm form)
    {
        _preparedDataRoom?.Dispose();
        _preparedDataRoom = form;
    }

    private static void ResetEnvironmentCards(Control root)
    {
        foreach (Control control in root.Controls)
        {
            if (control is EnvironmentCard card)
                card.ResetInteractionState();
            if (control.HasChildren)
                ResetEnvironmentCards(control);
        }
    }

    private void BuildLayout(AppThemePalette palette)
    {
        Color background = palette.IsDark ? palette.SurfaceDark : palette.SurfaceLight;
        BackColor = background;
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(26, 14, 26, 22),
            BackColor = background
        };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        cards.Controls.Add(CreateEnvironmentCard(
            "DATA ROOM",
            "Crea e gestisci condivisioni, autorizzazioni, scadenze e registri di attività.",
            "folder-open",
            palette,
            OpenDataRoom), 0, 0);
        cards.Controls.Add(CreateEnvironmentCard(
            "RASSEGNA STAMPA",
            "Pubblica, aggiorna e consulta le rassegne stampa destinate allo Studio.",
            "audit",
            palette,
            OpenPressReview), 1, 0);
        Controls.Add(cards);
    }

    private static Control CreateEnvironmentCard(
        string title,
        string description,
        string icon,
        AppThemePalette palette,
        Action action)
    {
        var card = new EnvironmentCard(palette, action)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(12),
            Padding = new Padding(24, 18, 24, 18),
            BackColor = Color.White
        };
        var symbol = new PictureBox
        {
            Dock = DockStyle.Top,
            Height = 58,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Image = CreateEnvironmentIcon(icon, palette.Primary)
        };
        var heading = new UnderlinedHeadingLabel(palette.InstitutionalAccent)
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = palette.Primary,
            BackColor = Color.Transparent
        };
        var text = new CardDescriptionLabel
        {
            Text = description,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(70, 70, 70),
            BackColor = Color.Transparent,
            Padding = new Padding(20, 8, 20, 8)
        };
        card.Controls.Add(text);
        card.Controls.Add(heading);
        card.Controls.Add(symbol);
        card.RegisterInteractiveChild(symbol);
        card.RegisterInteractiveChild(heading);
        card.RegisterInteractiveChild(text);
        return card;
    }

    private static Bitmap CreateEnvironmentIcon(string type, Color color)
    {
        var bitmap = new Bitmap(52, 52);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var pen = new Pen(color, 2.4F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        if (type == "folder-open")
        {
            graphics.DrawLine(pen, 8, 15, 23, 15);
            graphics.DrawLine(pen, 23, 15, 28, 20);
            graphics.DrawRectangle(pen, 7, 20, 38, 24);
            graphics.DrawLine(pen, 14, 38, 39, 27);
        }
        else
        {
            graphics.DrawRectangle(pen, 11, 7, 30, 38);
            graphics.DrawLine(pen, 17, 17, 35, 17);
            graphics.DrawLine(pen, 17, 25, 35, 25);
            graphics.DrawLine(pen, 17, 33, 30, 33);
        }
        return bitmap;
    }

    private sealed class EnvironmentCard : Panel
    {
        private readonly AppThemePalette _palette;
        private readonly Action _action;
        private readonly List<Control> _interactiveChildren = new();
        private bool _hovered;
        private bool _pressed;

        public EnvironmentCard(AppThemePalette palette, Action action)
        {
            _palette = palette;
            _action = action;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Resize += (_, _) => UpdateShape();
            Click += (_, _) => _action();
            MouseEnter += (_, _) => SetHovered(true);
            MouseLeave += (_, _) => CheckPointerOutside();
            MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                _pressed = true;
                UpdateAppearance();
            };
            MouseUp += (_, _) =>
            {
                _pressed = false;
                UpdateAppearance();
            };
        }

        public void RegisterInteractiveChild(Control control)
        {
            _interactiveChildren.Add(control);
            control.BackColor = CurrentBackground;
            control.Cursor = Cursors.Hand;
            control.Click += (_, _) => _action();
            control.MouseEnter += (_, _) => SetHovered(true);
            control.MouseLeave += (_, _) => CheckPointerOutside();
            control.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                _pressed = true;
                UpdateAppearance();
            };
            control.MouseUp += (_, _) =>
            {
                _pressed = false;
                UpdateAppearance();
            };
        }

        private void CheckPointerOutside()
        {
            BeginInvoke(() =>
            {
                if (!RectangleToScreen(ClientRectangle).Contains(Cursor.Position))
                    SetHovered(false);
            });
        }

        private void SetHovered(bool value)
        {
            if (_hovered == value) return;
            _hovered = value;
            UpdateAppearance();
        }

        public void ResetInteractionState()
        {
            _pressed = false;
            _hovered = false;
            Capture = false;
            UpdateAppearance();
        }

        private Color CurrentBackground =>
            _pressed
                ? Color.FromArgb(230, 235, 242)
                : _hovered
                    ? Color.FromArgb(241, 244, 248)
                    : Color.White;

        private void UpdateAppearance()
        {
            BackColor = CurrentBackground;
            foreach (Control control in _interactiveChildren)
                control.BackColor = CurrentBackground;
            Invalidate(true);
        }

        private void UpdateShape()
        {
            if (Width <= 0 || Height <= 0) return;
            using var path = RoundedRectangle(
                new Rectangle(0, 0, Width, Height),
                12);
            Region?.Dispose();
            Region = new Region(path);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(CurrentBackground);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var border = new Pen(
                _hovered ? _palette.Primary : Color.FromArgb(194, 198, 204),
                _hovered ? 1.8F : 1.2F);
            using var path = RoundedRectangle(
                new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3)),
                11);
            e.Graphics.DrawPath(border, path);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRectangle(
            Rectangle bounds,
            int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class UnderlinedHeadingLabel : Label
    {
        private readonly Color _accent;
        private readonly Font _displayFont = AppTypography.Create(18F, FontStyle.Bold);

        public UnderlinedHeadingLabel(Color accent)
        {
            _accent = accent;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var textBrush = new SolidBrush(ForeColor);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Near,
                FormatFlags = StringFormatFlags.NoWrap
            };
            e.Graphics.DrawString(
                Text,
                _displayFont,
                textBrush,
                new RectangleF(0, 0, Width, Height - 5),
                format);
            Size textSize = TextRenderer.MeasureText(
                Text,
                _displayFont,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int lineWidth = Math.Min(ClientSize.Width - 12, textSize.Width + 10);
            int left = (ClientSize.Width - lineWidth) / 2;
            using var pen = new Pen(_accent, 3F)
            {
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round
            };
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.DrawLine(pen, left, Height - 3, left + lineWidth, Height - 3);
        }
    }

    private sealed class CardDescriptionLabel : Control
    {
        private readonly Font _displayFont = AppTypography.Create(12F);

        public CardDescriptionLabel()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var brush = new SolidBrush(ForeColor);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.Word,
                FormatFlags = StringFormatFlags.LineLimit
            };
            Rectangle content = new(
                Padding.Left,
                Padding.Top,
                Math.Max(0, Width - Padding.Horizontal),
                Math.Max(0, Height - Padding.Vertical));
            e.Graphics.DrawString(Text, _displayFont, brush, content, format);
        }
    }

    private void OpenDataRoom()
    {
        Hide();
        if (_preparedDataRoom is null || _preparedDataRoom.IsDisposed)
            _preparedDataRoom = _services.GetRequiredService<MainForm>();

        MainForm form = _preparedDataRoom;
        form.PrepareForDisplay();
        form.ShowDialog();
        if (form.ExitApplicationRequested)
        {
            _preparedDataRoom = null;
            _exitConfirmed = true;
            Close();
            return;
        }
        Show();
        Activate();
    }

    private void OpenPressReview()
    {
        Hide();
        using PressReviewForm form = _services.GetRequiredService<PressReviewForm>();
        form.ShowDialog();
        if (form.ExitApplicationRequested)
        {
            _exitConfirmed = true;
            Close();
            return;
        }
        Show();
        Activate();
    }

    private void ConfirmExit(object? sender, FormClosingEventArgs e)
    {
        if (_exitConfirmed || e.CloseReason != CloseReason.UserClosing) return;
        if (AppMessageBox.Show(
                "Confermi di voler chiudere Document Hub?",
                "Conferma uscita",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                this) != DialogResult.Yes)
            e.Cancel = true;
    }
}
