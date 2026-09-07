namespace SharePointToolbox.UI;

public sealed class ConnectionProgressForm : ShapedStartupSplashForm
{
    public ConnectionProgressForm(
        Color? progressColor = null,
        Color? trackColor = null,
        Color? institutionalAccent = null)
        : base(showInTaskbar: false)
    {
    }
}
