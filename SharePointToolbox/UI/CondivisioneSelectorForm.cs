using System;
using System.Collections.Generic; // Necessario per la lista
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using SharePointToolbox.UI.Theming;

namespace SharePointToolbox.UI;

public class CondivisioneSelectorForm : InstitutionalMaterialForm
{
    private sealed record SharingRowData(string Id, string RequestedBy, string Notes);

    private InstitutionalMaterialTextBoxEdit txtSearch;
    private MaterialButton btnModifica;
    private readonly MainForm.DashboardActionButton _refreshButton;
    private ListView listView;
    private ListViewItem? _hoveredItem;
    private readonly SharingHoverPopup _sharingPopup = new();
    private static readonly Color HoverColor = Color.FromArgb(201, 183, 106);

    // Lista di supporto per filtrare i dati
    private List<ListViewItem> _masterList = new List<ListViewItem>();

    public string SelectedItemId { get; private set; } = string.Empty;
    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Func<Task>? RefreshRequested { get; set; }

    public CondivisioneSelectorForm()
    {
        // Impostazioni Generali
        this.Text = "Archivio storico delle condivisioni attive";
        this.ClientSize = new Size(830, 500);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Sizable = false;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        MaterialSkinManager.Instance.AddFormToManage(this);

        // 1. Barra di ricerca
        txtSearch = new InstitutionalMaterialTextBoxEdit
        {
            Location = new Point(15, 80),
            Size = new Size(750, UiMetrics.MaterialInputHeight),
            Hint = "Filtra o cerca una condivisione per nome...",
            UseTallSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        txtSearch.TextChanged += TxtSearch_TextChanged;
        this.Controls.Add(txtSearch);

        _refreshButton = new MainForm.DashboardActionButton
        {
            Text = string.Empty,
            Icon = MainForm.CreateButtonIcon("refresh"),
            PrimaryColor = AppThemeManager.PopupActiveText,
            PrimaryForeColor = Color.White,
            FixedWidth = 34
        };
        _refreshButton.Size = new Size(34, 34);
        _refreshButton.Location = new Point(781, 87);
        _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refreshButton.Click += async (_, _) => await RefreshListAsync();
        new ToolTip().SetToolTip(_refreshButton, "Aggiorna l’elenco da Microsoft 365");
        Controls.Add(_refreshButton);

        // 2. ListView
        listView = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            Location = new Point(15, 140),
            Size = new Size(800, 298),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            GridLines = true,
            HideSelection = false,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(32, 32, 32)
        };
        listView.Columns.Add("Nome condivisione", 360);
        listView.Columns.Add("Data creazione", 180);
        listView.Columns.Add("Scadenza", 240);
        InstitutionalSelectionStyle.Apply(listView);
        listView.HandleCreated += (_, _) => EnforceListBackground();
        listView.BackColorChanged += (_, _) =>
        {
            if (listView.BackColor != Color.White)
                EnforceListBackground();
        };
        Shown += (_, _) => EnforceListBackground();
        Activated += (_, _) => EnforceListBackground();
        FormClosed += (_, _) => _sharingPopup.Dispose();

        listView.DoubleClick += (s, e) => SelezionaEChiudi();
        listView.MouseMove += ListView_MouseMove;
        listView.MouseLeave += (_, _) => ClearHoveredItem();
        this.Controls.Add(listView);

        // 4. Bottone "MODIFICA" - Fucsia (Accent) e Arrotondato
        btnModifica = new MaterialButton
        {
            Text = "APRI",
            AutoSize = false,
            Size = new Size(UiMetrics.ButtonWidth, UiMetrics.ButtonHeight),
            Anchor = AnchorStyles.Bottom,
            Type = MaterialButton.MaterialButtonType.Contained,
            UseAccentColor = false
        };

        int xPos = (this.ClientSize.Width / 2) - (btnModifica.Width / 2);
        btnModifica.Location = new Point(xPos, 446);

        btnModifica.Click += (s, e) => SelezionaEChiudi();
        this.Controls.Add(btnModifica);
        btnModifica.Left = (ClientSize.Width - btnModifica.Width) / 2;
    }

    private void EnforceListBackground()
    {
        if (listView.IsDisposed) return;
        if (listView.BackColor != Color.White)
            listView.BackColor = Color.White;
        if (!listView.IsHandleCreated) return;

        int white = ColorTranslator.ToWin32(Color.White);
        SendMessage(listView.Handle, 0x1001, IntPtr.Zero, (IntPtr)white);
        SendMessage(listView.Handle, 0x1026, IntPtr.Zero, (IntPtr)white);
        listView.Invalidate();
    }

    private async Task RefreshListAsync()
    {
        if (RefreshRequested is null) return;
        string currentFilter = txtSearch.Text;
        _refreshButton.Enabled = false;
        UseWaitCursor = true;
        ClearHoveredItem();
        try
        {
            await RefreshRequested();
            txtSearch.Text = currentFilter;
            ApplyCurrentFilter();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(
                $"Aggiornamento dell’elenco non riuscito:\r\n{ex.Message}",
                "Aggiornamento non riuscito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning,
                this);
        }
        finally
        {
            UseWaitCursor = false;
            _refreshButton.Enabled = true;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr windowHandle,
        int message,
        IntPtr parameter,
        IntPtr value);

    private void SelezionaEChiudi()
    {
        if (listView.SelectedItems.Count > 0)
        {
            SelectedItemId = listView.SelectedItems[0].Tag is SharingRowData data
                ? data.Id
                : listView.SelectedItems[0].Tag?.ToString() ?? string.Empty;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            AppMessageBox.Show("Seleziona una condivisione dall'elenco per procedere.", "Attenzione", icon: MessageBoxIcon.Information, owner: this);
        }
    }

    // Filtro di ricerca VERO: nasconde gli elementi non corrispondenti
    private void TxtSearch_TextChanged(object? sender, EventArgs e)
    {
        ApplyCurrentFilter();
    }

    private void ApplyCurrentFilter()
    {
        string filter = txtSearch.Text.ToLower();

        ClearHoveredItem();
        listView.BeginUpdate(); // Evita il flickering durante l'aggiornamento
        listView.Items.Clear();

        // Filtra la lista master e riaggiungi solo ciò che serve
        var filteredItems = _masterList.Where(i => i.Text.ToLower().Contains(filter)).ToArray();
        listView.Items.AddRange(filteredItems);

        listView.EndUpdate();
    }

    public void ClearCondivisioni()
    {
        ClearHoveredItem();
        _masterList.Clear();
        listView.Items.Clear();
    }

    private void ListView_MouseMove(object? sender, MouseEventArgs e)
    {
        ListViewItem? item = listView.GetItemAt(e.X, e.Y);
        if (item is null)
        {
            ClearHoveredItem();
            return;
        }

        if (!ReferenceEquals(item, _hoveredItem))
        {
            ClearHoveredItem();
            _hoveredItem = item;
            if (!_hoveredItem.Selected)
                _hoveredItem.BackColor = HoverColor;
        }

        string creationDate = item.SubItems.Count > 1 ? item.SubItems[1].Text : "-";
        string expiration = item.SubItems.Count > 2 ? item.SubItems[2].Text : "-";
        SharingRowData? rowData = item.Tag as SharingRowData;
        string details =
            $"Nome condivisione: {item.Text}\n" +
            $"Creata il: {creationDate}\n" +
            $"Scadenza: {expiration}\n" +
            $"Richiesto da: {DisplayValue(rowData?.RequestedBy)}\n" +
            $"Note: {DisplayValue(rowData?.Notes)}";
        _sharingPopup.ShowAt(Cursor.Position, details, this);
    }

    private void ClearHoveredItem()
    {
        _sharingPopup.Hide();
        if (_hoveredItem is not null)
        {
            _hoveredItem.BackColor = Color.Empty;
            _hoveredItem = null;
        }
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    // Metodo helper per aggiungere dati alla master list e alla vista
    public void AddCondivisione(
        string nome,
        string data,
        string scadenza,
        string id,
        string richiestoDa,
        string note)
    {
        var item = new ListViewItem(nome);
        item.SubItems.Add(data);
        item.SubItems.Add(scadenza);
        item.Tag = new SharingRowData(id, richiestoDa, note);

        _masterList.Add(item); // Aggiungi alla memoria interna
        listView.Items.Add(item); // Aggiungi alla vista
    }

    private sealed class SharingHoverPopup : Form
    {
        private const int HorizontalPadding = 14;
        private const int VerticalPadding = 10;
        private const int TitleHeight = 24;
        private string _details = string.Empty;

        public SharingHoverPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            DoubleBuffered = true;
            Font = SystemFonts.MessageBoxFont;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= 0x00000080 | 0x08000000;
                return parameters;
            }
        }

        public void ShowAt(Point cursorScreenPosition, string details, Form owner)
        {
            if (!string.Equals(_details, details, StringComparison.Ordinal))
            {
                _details = details;
                Size textSize = TextRenderer.MeasureText(
                    details,
                    Font,
                    new Size(410, 0),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                ClientSize = new Size(
                    Math.Clamp(textSize.Width + (HorizontalPadding * 2), 250, 440),
                    textSize.Height + TitleHeight + (VerticalPadding * 2));
                UpdateShape();
            }

            Rectangle workArea = Screen.FromPoint(cursorScreenPosition).WorkingArea;
            int left = cursorScreenPosition.X + 8;
            int top = cursorScreenPosition.Y - Height - 6;
            left = Math.Clamp(left, workArea.Left, Math.Max(workArea.Left, workArea.Right - Width));
            top = Math.Clamp(top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - Height));
            Location = new Point(left, top);

            if (!Visible)
                Show(owner);
            else
                Invalidate();
        }

        private void UpdateShape()
        {
            using var path = new GraphicsPath();
            path.AddRectangle(new Rectangle(0, 0, Width - 1, Height - 1));
            Region?.Dispose();
            Region = new Region(path);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var lightBorderPen = new Pen(Color.FromArgb(198, 198, 198));
            using var institutionalGoldBrush = new SolidBrush(Color.FromArgb(201, 183, 106));
            using var iconBrush = new SolidBrush(Color.FromArgb(62, 88, 133));
            using var iconTextBrush = new SolidBrush(Color.White);
            using var boldFont = new Font(Font, FontStyle.Bold);
            using var centeredText = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            int bodyBottom = Height - 2;
            e.Graphics.DrawLine(lightBorderPen, 0, 0, Width - 2, 0);
            e.Graphics.DrawLine(lightBorderPen, Width - 2, 0, Width - 2, bodyBottom);
            e.Graphics.DrawLine(lightBorderPen, Width - 2, bodyBottom, 0, bodyBottom);

            e.Graphics.FillRectangle(
                institutionalGoldBrush,
                0,
                0,
                4,
                Height);
            var iconBounds = new Rectangle(HorizontalPadding, VerticalPadding, 16, 16);
            var titleBounds = new Rectangle(
                HorizontalPadding + 23,
                VerticalPadding,
                Width - HorizontalPadding - 23,
                16);
            var iconTextBounds = new Rectangle(
                iconBounds.X,
                iconBounds.Y + 1,
                iconBounds.Width,
                iconBounds.Height);
            e.Graphics.FillEllipse(iconBrush, iconBounds);
            e.Graphics.DrawString("i", boldFont, iconTextBrush, iconTextBounds, centeredText);
            TextRenderer.DrawText(
                e.Graphics,
                "Dettagli condivisione",
                boldFont,
                titleBounds,
                Color.FromArgb(62, 88, 133),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                _details,
                Font,
                new Rectangle(
                    HorizontalPadding,
                    VerticalPadding + TitleHeight,
                    Width - (HorizontalPadding * 2),
                    Height - TitleHeight - (VerticalPadding * 2)),
                Color.FromArgb(32, 32, 32),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        }
    }
}
