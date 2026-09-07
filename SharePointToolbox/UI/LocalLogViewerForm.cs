using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using SharePointToolbox.Helpers;
using SharePointToolbox.UI.Theming;

namespace SharePointToolbox.UI;

public sealed class LocalLogViewerForm : InstitutionalMaterialForm
{
    private readonly ListView _entries = new();
    private readonly AppThemeManager _themeManager;

    public LocalLogViewerForm(AppThemeManager themeManager)
    {
        _themeManager = themeManager;
        Text = "Log locale";
        ClientSize = new Size(920, 520);
        StartPosition = FormStartPosition.CenterParent;
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        themeManager.ApplyTo(this);

        _entries.Bounds = new Rectangle(UiMetrics.DialogPadding, 78, 872, 360);
        _entries.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        _entries.View = View.Details;
        _entries.FullRowSelect = true;
        _entries.GridLines = false;
        _entries.HideSelection = false;
        _entries.Columns.Add("Data e ora", 150);
        _entries.Columns.Add("Messaggio", 690);
        InstitutionalSelectionStyle.Apply(_entries);

        var refreshButton = CreateButton("Aggiorna");
        var closeButton = CreateButton("Chiudi");
        int buttonsWidth = UiMetrics.ButtonWidth * 2 + UiMetrics.Spacing;
        int x = (ClientSize.Width - buttonsWidth) / 2;
        refreshButton.Location = new Point(x, 458);
        closeButton.Location = new Point(x + UiMetrics.ButtonWidth + UiMetrics.Spacing, 458);
        refreshButton.Click += (_, _) => LoadEntries();
        closeButton.Click += (_, _) => Close();

        Controls.Add(_entries);
        Controls.Add(refreshButton);
        Controls.Add(closeButton);
        Shown += (_, _) =>
        {
            LoadEntries();
            UiLogger.EntryAdded += OnEntryAdded;
        };
        FormClosed += (_, _) => UiLogger.EntryAdded -= OnEntryAdded;
    }

    private void LoadEntries()
    {
        _entries.BeginUpdate();
        _entries.Items.Clear();
        foreach (UiLogger.Entry entry in UiLogger.GetSnapshot()) AddEntry(entry);
        _entries.EndUpdate();
        EnsureLatestVisible();
    }

    private void OnEntryAdded(UiLogger.Entry entry)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(() => OnEntryAdded(entry));
            return;
        }

        AddEntry(entry);
        EnsureLatestVisible();
    }

    private void AddEntry(UiLogger.Entry entry)
    {
        var item = new ListViewItem(entry.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"));
        item.SubItems.Add(entry.Message);
        _entries.Items.Add(item);
    }

    private void EnsureLatestVisible()
    {
        if (_entries.Items.Count > 0) _entries.Items[^1].EnsureVisible();
    }

    private static MaterialButton CreateButton(string text) => new()
    {
        Text = text,
        AutoSize = false,
        Size = new Size(UiMetrics.ButtonWidth, UiMetrics.ButtonHeight),
        Type = MaterialButton.MaterialButtonType.Contained,
        HighEmphasis = true,
        UseAccentColor = false
    };
}
