using SharePointToolbox.Microsoft365;
using SharePointToolbox.UI.Theming;

namespace SharePointToolbox.UI;

public sealed class PressReviewArchiveForm : InstitutionalMaterialForm
{
    private readonly PressReviewService _service;
    private readonly AppThemePalette _palette;
    private readonly ListView _reviews = new();
    private readonly MainForm.DashboardActionButton _openButton;
    private readonly Label _status = new();
    public DateTime? SelectedDate { get; private set; }

    public PressReviewArchiveForm(
        PressReviewService service,
        AppThemeManager theme)
    {
        _service = service;
        _palette = theme.Palette;
        Text = "Archivio rassegne stampa";
        ClientSize = new Size(600, 520);
        StartPosition = FormStartPosition.CenterParent;
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = false;
        theme.ApplyTo(this);
        _openButton = CreateArchiveButton("Apri e modifica", "folder-open");
        BuildLayout();
        Shown += async (_, _) => await LoadArchiveAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(16, 12, 16, 16),
            BackColor = _palette.IsDark ? _palette.SurfaceDark : _palette.SurfaceLight
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        root.Controls.Add(new Label
        {
            Text = "Rassegne pubblicate",
            Dock = DockStyle.Fill,
            Font = AppTypography.Create(14F, FontStyle.Bold),
            ForeColor = _palette.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _reviews.Dock = DockStyle.Fill;
        _reviews.View = View.Details;
        _reviews.FullRowSelect = true;
        _reviews.HideSelection = false;
        _reviews.MultiSelect = false;
        _reviews.BackColor = Color.White;
        _reviews.Columns.Add("Data rassegna", 310);
        _reviews.Columns.Add("Documenti", 220);
        InstitutionalSelectionStyle.Apply(_reviews);
        _reviews.SelectedIndexChanged += (_, _) =>
            _openButton.Enabled = _reviews.SelectedItems.Count == 1;
        _reviews.DoubleClick += (_, _) => OpenSelected();
        root.Controls.Add(_reviews, 0, 1);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        _openButton.Width = 175;
        _openButton.Enabled = false;
        _openButton.Click += (_, _) => OpenSelected();
        MainForm.DashboardActionButton close = CreateArchiveButton("Annulla");
        close.Width = 130;
        close.Click += (_, _) => Close();
        actions.Controls.Add(_openButton);
        actions.Controls.Add(close);
        root.Controls.Add(actions, 0, 2);

        _status.Dock = DockStyle.Fill;
        _status.Font = AppTypography.Create(9.5F);
        _status.ForeColor = _palette.IsDark ? _palette.SecondaryTextDark : _palette.SecondaryTextLight;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_status, 0, 3);
        Controls.Add(root);
    }

    private MainForm.DashboardActionButton CreateArchiveButton(
        string text,
        string? icon = null)
    {
        return new MainForm.DashboardActionButton
        {
            Text = text,
            Icon = icon is null ? null : MainForm.CreateButtonIcon(icon),
            ExtraHorizontalPadding = 6,
            PrimaryColor = _palette.DashboardButtonBackground,
            PrimaryForeColor = _palette.DashboardButtonForeground,
            DangerColor = _palette.Danger,
            DisabledBackColor = _palette.IsDark
                ? Color.FromArgb(70, 70, 70)
                : Color.FromArgb(228, 228, 228),
            DisabledForeColor = Color.FromArgb(145, 145, 145)
        };
    }

    private async Task LoadArchiveAsync()
    {
        UseWaitCursor = true;
        _status.Text = "Caricamento dell’archivio…";
        try
        {
            IReadOnlyList<PressReviewService.ReviewEntry> entries =
                await _service.GetArchiveAsync();
            _reviews.BeginUpdate();
            _reviews.Items.Clear();
            foreach (PressReviewService.ReviewEntry entry in entries)
            {
                var item = new ListViewItem(entry.Date.ToString("dddd d MMMM yyyy"))
                {
                    Tag = entry.Date
                };
                item.SubItems.Add(entry.FileCount.ToString());
                _reviews.Items.Add(item);
            }
            _reviews.EndUpdate();
            _status.Text = entries.Count == 0
                ? "Nessuna rassegna pubblicata."
                : $"Rassegne disponibili: {entries.Count}";
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(ex.Message, "Archivio non disponibile",
                MessageBoxButtons.OK, MessageBoxIcon.Error, this);
            _status.Text = "Caricamento dell’archivio non riuscito.";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void OpenSelected()
    {
        if (_reviews.SelectedItems.Count != 1) return;
        if (_reviews.SelectedItems[0].Tag is not DateTime date) return;
        SelectedDate = date;
        DialogResult = DialogResult.OK;
        Close();
    }
}
