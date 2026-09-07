using SharePointToolbox.UI.Theming;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SharePointToolbox.UI;

/// <summary>
/// Splash applicativo sagomato tramite alpha per-pixel.
/// Tutti i contenuti dinamici vengono renderizzati in un'unica bitmap,
/// preservando le curve trasparenti della sagoma.
/// </summary>
public abstract class ShapedStartupSplashForm : Form
{
    private static readonly Size SplashSize = new(513, 405);
    private readonly Bitmap _shell;
    private readonly Font _studioFont = AppTypography.Create(20F, FontStyle.Bold);
    private readonly Font _productFont = AppTypography.Create(21F, FontStyle.Bold);
    private readonly Font _versionFont = AppTypography.Create(9.5F);
    private readonly Font _messageFont = AppTypography.Create(10F);
    private readonly Font _percentageFont = AppTypography.Create(10.5F, FontStyle.Bold);
    private int _authorizationPercentage;
    private int _initializationPercentage;
    private string _authorizationMessage = "Verifica dell'autorizzazione…";
    private string _initializationMessage = "Preparazione Document Hub in attesa…";

    protected ShapedStartupSplashForm(bool showInTaskbar)
    {
        string shellPath = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "StartupSplash.png");
        if (!File.Exists(shellPath))
            throw new FileNotFoundException("Risorsa grafica dello splash non trovata.", shellPath);

        using (Image source = Image.FromFile(shellPath))
            _shell = new Bitmap(source);

        string iconPath = Path.Combine(
            AppContext.BaseDirectory,
            "Resources",
            "AppIcon.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        Text = "Giovanardi Studio Legale Document Hub";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ClientSize = SplashSize;
        MinimumSize = Size.Empty;
        MaximumSize = Size.Empty;
        AutoScaleMode = AutoScaleMode.None;
        ControlBox = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = showInTaskbar;
        TopMost = false;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        Shown += (_, _) => RenderLayeredWindow();
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams parameters = base.CreateParams;
            parameters.ExStyle |= 0x00080000; // WS_EX_LAYERED
            return parameters;
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        Rectangle workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(
            workingArea.Left + (workingArea.Width - Width) / 2,
            workingArea.Top + (workingArea.Height - Height) / 2);
        base.OnLoad(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RenderLayeredWindow();
    }

    public void ReportAuthorization(int percentage, string message) =>
        ReportPhase(true, percentage, message);

    public void ReportInitialization(int percentage, string message) =>
        ReportPhase(false, percentage, message);

    private void ReportPhase(bool authorization, int percentage, string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => ReportPhase(authorization, percentage, message));
            return;
        }

        int normalizedPercentage = Math.Clamp(percentage, 0, 100);
        string normalizedMessage = string.IsNullOrWhiteSpace(message)
            ? "Operazione in corso…"
            : message.Trim();
        if (authorization)
        {
            _authorizationPercentage = normalizedPercentage;
            _authorizationMessage = normalizedMessage;
        }
        else
        {
            _initializationPercentage = normalizedPercentage;
            _initializationMessage = normalizedMessage;
        }
        if (IsHandleCreated && Visible)
            RenderLayeredWindow();
    }

    private void RenderLayeredWindow()
    {
        if (IsDisposed || !IsHandleCreated) return;

        using var rendered = new Bitmap(
            SplashSize.Width,
            SplashSize.Height,
            PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(rendered))
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceOver;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            graphics.DrawImage(_shell, new Rectangle(Point.Empty, SplashSize));
            DrawContent(graphics);
        }

        ApplyLayeredBitmap(rendered);
    }

    private void DrawContent(Graphics graphics)
    {
        Color white = Color.FromArgb(248, 248, 248);
        Color mutedWhite = Color.FromArgb(228, 235, 243);
        Color gold = Color.FromArgb(201, 183, 106);
        Color track = Color.FromArgb(48, 42, 20);
        using var whiteBrush = new SolidBrush(white);
        using var mutedWhiteBrush = new SolidBrush(mutedWhite);
        using var goldBrush = new SolidBrush(gold);
        using var trackBrush = new SolidBrush(track);
        using var centered = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter
        };
        using var left = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        using var right = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };

        RectangleF studioBounds = new(26, 176, 414, 27);
        graphics.DrawString(
            "GIOVANARDI STUDIO LEGALE",
            _studioFont,
            whiteBrush,
            studioBounds,
            centered);
        graphics.FillRectangle(goldBrush, 90, 205, 285, 2);

        graphics.DrawString(
            "DOCUMENT HUB",
            _productFont,
            whiteBrush,
            new RectangleF(26, 210, 414, 32),
            centered);
        graphics.DrawString(
            $"Versione {Application.ProductVersion.Split('+')[0]}",
            _versionFont,
            mutedWhiteBrush,
            new RectangleF(290, 370, 152, 18),
            right);

        DrawProgress(graphics, _authorizationMessage, _authorizationPercentage, 254,
            whiteBrush, goldBrush, trackBrush, whiteBrush, left, centered);
        DrawProgress(graphics, _initializationMessage, _initializationPercentage, 314,
            whiteBrush, goldBrush, trackBrush, whiteBrush, left, centered);
    }

    private void DrawProgress(
        Graphics graphics,
        string message,
        int percentage,
        float messageTop,
        Brush messageBrush,
        Brush fillBrush,
        Brush trackBrush,
        Brush percentageBrush,
        StringFormat left,
        StringFormat centered)
    {
        const float progressLeft = 38;
        const float progressWidth = 390;
        const float progressHeight = 16;
        float progressTop = messageTop + 24;
        graphics.DrawString(
            message,
            _messageFont,
            messageBrush,
            new RectangleF(progressLeft, messageTop, progressWidth, 22),
            left);
        using GraphicsPath trackPath = RoundedRectangle(
            new RectangleF(progressLeft, progressTop, progressWidth, progressHeight),
            progressHeight / 2);
        graphics.FillPath(trackBrush, trackPath);
        float fillWidth = progressWidth * percentage / 100F;
        if (fillWidth > 0)
        {
            using GraphicsPath fillPath = RoundedRectangle(
                new RectangleF(
                    progressLeft,
                    progressTop,
                    Math.Max(progressHeight, fillWidth),
                    progressHeight),
                progressHeight / 2);
            Region previousClip = graphics.Clip;
            graphics.SetClip(
                new RectangleF(progressLeft, progressTop, fillWidth, progressHeight),
                CombineMode.Intersect);
            graphics.FillPath(fillBrush, fillPath);
            graphics.Clip = previousClip;
            previousClip.Dispose();
        }

        graphics.DrawString(
            $"{percentage}%",
            _percentageFont,
            percentageBrush,
            new RectangleF(progressLeft, progressTop - 5, progressWidth, progressHeight + 12),
            centered);
    }

    private static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        float diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(
            bounds.Right - diameter,
            bounds.Bottom - diameter,
            diameter,
            diameter,
            0,
            90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void ApplyLayeredBitmap(Bitmap bitmap)
    {
        IntPtr screenDeviceContext = GetDC(IntPtr.Zero);
        IntPtr memoryDeviceContext = CreateCompatibleDC(screenDeviceContext);
        IntPtr bitmapHandle = IntPtr.Zero;
        IntPtr previousObject = IntPtr.Zero;
        try
        {
            bitmapHandle = bitmap.GetHbitmap(Color.FromArgb(0));
            previousObject = SelectObject(memoryDeviceContext, bitmapHandle);
            var destination = new NativePoint(Left, Top);
            var source = new NativePoint(0, 0);
            var size = new NativeSize(bitmap.Width, bitmap.Height);
            var blend = new BlendFunction
            {
                BlendOp = 0,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = 1
            };
            UpdateLayeredWindow(
                Handle,
                screenDeviceContext,
                ref destination,
                ref size,
                memoryDeviceContext,
                ref source,
                0,
                ref blend,
                0x00000002);
        }
        finally
        {
            if (previousObject != IntPtr.Zero)
                SelectObject(memoryDeviceContext, previousObject);
            if (bitmapHandle != IntPtr.Zero)
                DeleteObject(bitmapHandle);
            DeleteDC(memoryDeviceContext);
            ReleaseDC(IntPtr.Zero, screenDeviceContext);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _shell.Dispose();
            _studioFont.Dispose();
            _productFont.Dispose();
            _versionFont.Dispose();
            _messageFont.Dispose();
            _percentageFont.Dispose();
        }
        base.Dispose(disposing);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;

        public NativePoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeSize
    {
        public int Width;
        public int Height;

        public NativeSize(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct BlendFunction
    {
        public byte BlendOp;
        public byte BlendFlags;
        public byte SourceConstantAlpha;
        public byte AlphaFormat;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UpdateLayeredWindow(
        IntPtr windowHandle,
        IntPtr destinationDeviceContext,
        ref NativePoint destination,
        ref NativeSize size,
        IntPtr sourceDeviceContext,
        ref NativePoint source,
        int colorKey,
        ref BlendFunction blend,
        int flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr windowHandle, IntPtr deviceContext);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr deviceContext);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr graphicObject);
}
