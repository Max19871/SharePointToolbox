using Microsoft.Extensions.Options;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using SharePointToolbox.Configuration;
using SharePointToolbox.Helpers;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.Models;
using SharePointToolbox.UI.Theming;
using System.Drawing;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using WinFormsPanel = System.Windows.Forms.Panel;

namespace SharePointToolbox.UI;

/// <summary>
/// Dashboard operativa ReaLTaiizor collegata ai servizi Microsoft 365.
/// La precedente UI MaterialSkin resta disponibile come fallback configurabile.
/// </summary>
public sealed class MainForm : MaterialForm
{
    private const int InstitutionalTopBorderHeight = 4;
    private const int MaterialStatusBarHeight = 24;
    private const int MaterialActionBarHeight = 40;
    private const int DashboardButtonHeight = 34;
    private const int DashboardSpacing = 8;
    private const int DashboardToolbarSpacing = 6;
    private const int DashboardPadding = 12;
    private readonly BrandingOptions _branding;
    private readonly AppThemePalette _palette;
    private readonly AuthenticationService _authenticationService;
    private readonly ApplicationAuthorizationSession _authorizationSession;
    private readonly SharePointService _sharePointService;
    private readonly SecurityOptions _security;
    private readonly AppThemeManager _themeManager;
    private readonly Dictionary<string, SharePointService.DriveItem> _folders = new();
    private readonly Dictionary<string, string> _folderStatuses = new();

    private readonly Label _siteName = CreateLabel("-", 10F, FontStyle.Regular);
    private readonly Label _siteStatus = CreateLabel("Verifica connessione in corso…", 10F, FontStyle.Regular);
    private readonly Label _userName = CreateLabel("Non autenticato", 10F, FontStyle.Regular);
    private readonly Label _userEmail = CreateLabel("-", 10F, FontStyle.Regular);
    private readonly ConnectionStatusIndicator _connectionBadge = new();
    private readonly MaterialRichTextDisplay _details = new()
    {
        Text = "Nessuna condivisione selezionata",
        Font = AppTypography.Create(10F),
        BorderStyle = BorderStyle.None,
        ReadOnly = true,
        DetectUrls = false,
        TabStop = false,
        ScrollBars = RichTextBoxScrollBars.None
    };
    private readonly Label _status = CreateLabel("Avvio della nuova interfaccia…", 9F, FontStyle.Regular);
    private readonly Label _connectionStatus = CreateLabel("Microsoft 365 non connesso", 9F, FontStyle.Regular);
    private readonly Label _userStatus = CreateLabel("Utente: -", 9F, FontStyle.Regular);
    private readonly Label _sharingStatus = CreateLabel("Condivisioni caricate: 0", 9F, FontStyle.Regular);
    private readonly Label _lastUpdateStatus = CreateLabel("Ultimo aggiornamento: -", 9F, FontStyle.Regular);
    private readonly ListView _participants = new();
    private readonly ImageList _participantIcons = new();
    private readonly DashboardActionButton _createButton = CreateButton("Nuova", false);
    private readonly DashboardActionButton _openButton = CreateButton("Apri", false);
    private readonly DashboardActionButton _addButton = CreateButton("Aggiungi utenti", false);
    private readonly DashboardActionButton _roleButton = CreateButton("Modifica ruolo", false);
    private readonly DashboardActionButton _practiceButton = CreateButton("Modifica condivisione", false);
    private readonly DashboardActionButton _removeButton = CreateButton("Rimuovi", true);
    private readonly DashboardActionButton _closeButton = CreateButton("Revoca accessi", true);
    private readonly DashboardActionButton _sendLinkButton = CreateButton("Invia link", false);
    private readonly DashboardActionButton _expirationButton = CreateSmallButton("Scadenze");
    private readonly DashboardActionButton _auditButton = CreateSmallButton("Registro audit");
    private readonly DashboardActionButton _localLogButton = CreateSmallButton("Log");
    private readonly DashboardActionButton _infoButton = CreateSmallButton("Informazioni");
    private TableLayoutPanel _root = null!;
    private FlowLayoutPanel _actionToolbar = null!;

    private AuthenticationService.CurrentUserProfile? _currentUser;
    private string _selectedPracticeId = string.Empty;
    private bool _connectionAttempted;
    private bool _operationInProgress;
    private bool _backHovered;
    private bool _canViewAudit;
    private bool _skipExitConfirmation;
    private bool _startupExpirationCheckCompleted;
    public bool ExitApplicationRequested { get; private set; }

    public MainForm(
        IOptions<BrandingOptions> branding,
        IOptions<SecurityOptions> security,
        AppThemeManager themeManager,
        AuthenticationService authenticationService,
        ApplicationAuthorizationSession authorizationSession,
        SharePointService sharePointService)
    {
        _branding = branding.Value;
        _security = security.Value;
        _palette = themeManager.Palette;
        _themeManager = themeManager;
        _authenticationService = authenticationService;
        _authorizationSession = authorizationSession;
        _sharePointService = sharePointService;
        _details.LockBackColor(ReaLTaiizor.Manager.MaterialSkinManager.Instance.CardsColor);

        Text = "Giovanardi Studio Legale Data Room";
        Opacity = 0D;
        ClientSize = new Size(820, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = true;
        themeManager.ApplyTo(this);
        ConfigureDashboardButtonPalette();
        ConfigureButtonIcons();
        BuildLayout();
        SetConnectedState(false);
        FormClosing += ConfirmApplicationExit;
        Activated += (_, _) => BeginInvoke(RestoreAfterPopup);
        Shown += async (_, _) =>
        {
            StabilizeInitialLayout();
            await ConnectOnStartupAsync();
            await CheckStartupExpirationsAfterDisplayAsync();
        };
    }

    private void BuildLayout()
    {
        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(DashboardPadding, 0, DashboardPadding, 3),
            ColumnCount = 1,
            RowCount = 3,
            BackColor = _palette.IsDark ? _palette.SurfaceDark : _palette.SurfaceLight
        };
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        Controls.Add(_root);
        _root.Controls.Add(BuildActionToolbar(), 0, 0);
        _root.Controls.Add(BuildContent(), 0, 1);
        _root.Controls.Add(BuildStatusBar(), 0, 2);
    }

    private Control BuildActionToolbar()
    {
        var toolbarHost = new WinFormsPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = Color.Transparent
        };

        _actionToolbar = new FlowLayoutPanel
        {
            Location = new Point(0, 3),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Height = DashboardButtonHeight + 4,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };

        foreach (DashboardActionButton button in new[]
                 {
                     _createButton, _openButton, _expirationButton, _infoButton
                 })
        {
            button.Margin = new Padding(0, 0, DashboardToolbarSpacing, 0);
            _actionToolbar.Controls.Add(button);
        }
        _localLogButton.Margin = new Padding(0, 0, DashboardToolbarSpacing, 0);
        _auditButton.Margin = new Padding(0, 0, DashboardToolbarSpacing, 0);

        _createButton.Click += async (_, _) => await RunOperationAsync(CreatePracticeAsync, "Creazione condivisione");
        _openButton.Click += async (_, _) => await RunOperationAsync(OpenPracticeAsync, "Apertura condivisione");
        _expirationButton.Click += async (_, _) =>
            await RunOperationAsync(CheckExpirationsManuallyAsync, "Controllo scadenze");
        _auditButton.Click += (_, _) => OpenAuditViewer();
        _localLogButton.Click += (_, _) => OpenLocalLogViewer();
        _infoButton.Click += (_, _) =>
        {
            using var dialog = new AboutForm(_branding);
            dialog.ShowDialog(this);
        };

        toolbarHost.Controls.Add(_actionToolbar);
        toolbarHost.Resize += (_, _) =>
        {
            _actionToolbar.SetBounds(
                0,
                Math.Max(0, (toolbarHost.ClientSize.Height - DashboardButtonHeight) / 2),
                toolbarHost.ClientSize.Width,
                DashboardButtonHeight + 2);
        };
        return toolbarHost;
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, Padding = new Padding(0, 0, 0, UiMetrics.ShadowClearance) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));

        MaterialCard sharePointCard = CreateCard();
        PictureBox microsoft365Icon = CreateHeaderIcon("microsoft365");
        Label microsoft365Title = AddCardTitle(sharePointCard, "Servizi Microsoft 365", x: 82);
        var microsoft365TitleFont = AppTypography.Create(15.5F, FontStyle.Bold);
        microsoft365Title.Font = microsoft365TitleFont;
        _siteName.Visible = false;
        _siteStatus.Location = new Point(82, 58);
        _siteStatus.Size = new Size(380, 24);
        _connectionBadge.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        int titleWidth = microsoft365Title.GetPreferredSize(Size.Empty).Width;
        _connectionBadge.Location = new Point(microsoft365Title.Left + titleWidth + 4, 21);
        _connectionBadge.Size = new Size(16, 16);
        sharePointCard.Controls.Add(microsoft365Icon);
        sharePointCard.Controls.Add(_siteStatus);
        sharePointCard.Controls.Add(_connectionBadge);
        void LayoutMicrosoft365Header()
        {
            if (Math.Abs(microsoft365Title.Font.Size - microsoft365TitleFont.Size) > 0.1F
                || microsoft365Title.Font.Style != FontStyle.Bold)
                microsoft365Title.Font = microsoft365TitleFont;

            int titleHeight = microsoft365Title.GetPreferredSize(Size.Empty).Height;
            int statusHeight = _siteStatus.GetPreferredSize(Size.Empty).Height;
            const int lineSpacing = 5;
            int groupHeight = titleHeight + lineSpacing + statusHeight;
            int groupTop = Math.Max(10, (sharePointCard.ClientSize.Height - groupHeight) / 2);

            microsoft365Icon.Top = Math.Max(0, (sharePointCard.ClientSize.Height - microsoft365Icon.Height) / 2);
            microsoft365Title.Top = groupTop;
            _siteStatus.Top = microsoft365Title.Top + titleHeight + lineSpacing;
            _connectionBadge.Left = microsoft365Title.Left + microsoft365Title.GetPreferredSize(Size.Empty).Width + 5;
            _connectionBadge.Top = microsoft365Title.Top + Math.Max(0, (titleHeight - _connectionBadge.Height) / 2);
        }
        sharePointCard.Layout += (_, _) => LayoutMicrosoft365Header();
        sharePointCard.Resize += (_, _) => LayoutMicrosoft365Header();

        MaterialCard userCard = CreateCard();
        PictureBox nameIcon = CreateIdentityIcon("person", _palette.Primary);
        PictureBox emailIcon = CreateIdentityIcon("email", _palette.Primary);
        var identityFont = AppTypography.Create(12.5F);
        _userName.Font = identityFont;
        _userName.Location = new Point(54, 0);
        _userName.Size = new Size(336, 26);
        _userName.TextAlign = ContentAlignment.MiddleLeft;
        _userEmail.Font = identityFont;
        _userEmail.Location = new Point(54, 0);
        _userEmail.Size = new Size(336, 26);
        _userEmail.TextAlign = ContentAlignment.MiddleLeft;
        userCard.Controls.Add(nameIcon);
        userCard.Controls.Add(emailIcon);
        userCard.Controls.Add(_userName);
        userCard.Controls.Add(_userEmail);
        void LayoutIdentityCard()
        {
            if (Math.Abs(_userName.Font.Size - identityFont.Size) > 0.1F)
                _userName.Font = identityFont;
            if (Math.Abs(_userEmail.Font.Size - identityFont.Size) > 0.1F)
                _userEmail.Font = identityFont;

            const int rowHeight = 26;
            const int rowSpacing = 8;
            int groupHeight = rowHeight * 2 + rowSpacing;
            int top = Math.Max(10, (userCard.ClientSize.Height - groupHeight) / 2);
            nameIcon.Location = new Point(UiMetrics.CardPadding, top + (rowHeight - nameIcon.Height) / 2);
            _userName.Top = top;
            _userEmail.Top = top + rowHeight + rowSpacing;
            emailIcon.Location = new Point(UiMetrics.CardPadding, _userEmail.Top + (rowHeight - emailIcon.Height) / 2);
            int labelWidth = Math.Max(120, userCard.ClientSize.Width - _userName.Left - UiMetrics.CardPadding);
            _userName.Width = labelWidth;
            _userEmail.Width = labelWidth;
        }
        userCard.Layout += (_, _) => LayoutIdentityCard();
        userCard.Resize += (_, _) => LayoutIdentityCard();

        MaterialCard actionsCard = CreateCard();
        Label actionsTitle = AddCardTitle(actionsCard, "Gestione condivisione", 13F);
        var actionsTitleFont = AppTypography.Create(13F, FontStyle.Bold);
        actionsTitle.Font = actionsTitleFont;
        _createButton.Location = new Point(UiMetrics.CardPadding, 54);
        _openButton.Location = new Point(_createButton.Right + UiMetrics.Spacing, 54);
        _createButton.Click += async (_, _) => await RunOperationAsync(CreatePracticeAsync, "Creazione condivisione");
        _openButton.Click += async (_, _) => await RunOperationAsync(OpenPracticeAsync, "Apertura condivisione");
        actionsCard.Controls.Add(_createButton);
        actionsCard.Controls.Add(_openButton);
        void LayoutPracticeActions()
        {
            if (Math.Abs(actionsTitle.Font.Size - actionsTitleFont.Size) > 0.1F
                || actionsTitle.Font.Style != FontStyle.Bold)
                actionsTitle.Font = actionsTitleFont;
            int buttonsWidth = _createButton.Width + UiMetrics.Spacing + _openButton.Width;
            int usableWidth = actionsCard.ClientSize.Width - UiMetrics.ShadowClearance;
            int left = Math.Max(UiMetrics.CardPadding, (usableWidth - buttonsWidth) / 2);
            _createButton.Left = left;
            _openButton.Left = _createButton.Right + UiMetrics.Spacing;
        }
        actionsCard.Layout += (_, _) => LayoutPracticeActions();
        actionsCard.Resize += (_, _) => LayoutPracticeActions();

        sharePointCard.Margin = new Padding(0, 0, UiMetrics.Spacing / 2, UiMetrics.ShadowClearance);
        userCard.Margin = new Padding(UiMetrics.Spacing / 2, 0, UiMetrics.Spacing / 2, UiMetrics.ShadowClearance);
        actionsCard.Margin = new Padding(UiMetrics.Spacing / 2, 0, UiMetrics.ShadowClearance, UiMetrics.ShadowClearance);
        header.Controls.Add(sharePointCard, 0, 0);
        header.Controls.Add(userCard, 1, 0);
        header.Controls.Add(actionsCard, 2, 0);
        return header;
    }

    private Control BuildContent()
    {
        Color surface = _palette.IsDark ? _palette.SurfaceDark : Color.White;
        var workspace = new WorkspaceSurface(
            surface,
            _palette.IsDark ? Color.FromArgb(78, 78, 78) : Color.FromArgb(205, 207, 210))
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(1)
        };
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0),
            BackColor = surface
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 69));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(CreateParticipantsCard(), 0, 0);
        content.Controls.Add(CreatePracticeDetailsCard(), 1, 0);
        workspace.Controls.Add(content);
        return workspace;
    }

    private WinFormsPanel CreateParticipantsCard()
    {
        Color surface = _palette.IsDark ? _palette.SurfaceDark : Color.White;
        var card = new WorkspaceSectionPanel(surface, Color.Empty)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = surface
        };
        WinFormsPanel header = CreateWorkspaceHeader("Autorizzazioni di accesso", 13F);
        Label accessTitle = (Label)header.Controls[0];
        var accessTitleFont = AppTypography.Create(13F, FontStyle.Bold);
        accessTitle.Font = accessTitleFont;
        card.Layout += (_, _) =>
        {
            if (Math.Abs(accessTitle.Font.Size - accessTitleFont.Size) > 0.1F
                || accessTitle.Font.Style != FontStyle.Bold)
                accessTitle.Font = accessTitleFont;
        };

        _participants.Location = new Point(DashboardPadding, 50);
        _participants.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _participants.Size = new Size(970, 490);
        _participants.View = View.Details;
        _participants.FullRowSelect = true;
        _participants.HideSelection = false;
        _participants.ShowItemToolTips = true;
        _participants.BorderStyle = BorderStyle.None;
        _participants.BackColor = surface;
        _participants.ForeColor = _palette.IsDark ? _palette.TextDark : _palette.TextLight;
        _participants.HandleCreated += (_, _) => ApplyParticipantsNativeColors(surface);
        _participantIcons.ImageSize = new Size(16, 16);
        _participantIcons.ColorDepth = ColorDepth.Depth32Bit;
        _participantIcons.Images.Add("internal", new Bitmap(16, 16));
        _participantIcons.Images.Add("external", CreateExternalAccessIcon(_palette.Warning));
        _participants.SmallImageList = _participantIcons;
        _participants.Columns.Add("Nome", 320);
        _participants.Columns.Add("Email", 420);
        _participants.Columns.Add("Ruolo", 120);
        InstitutionalSelectionStyle.Apply(_participants);
        ConfigureParticipantsContextMenu();
        _participants.SelectedIndexChanged += (_, _) => UpdateParticipantSelectionState();

        var actions = new WorkspaceActionFlowPanel(surface)
        {
            Location = new Point(0, 0),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Size = new Size(520, DashboardButtonHeight * 2 + DashboardSpacing + 10),
            WrapContents = true,
            Padding = new Padding(DashboardPadding, 5, DashboardPadding, 0),
            BackColor = surface
        };
        foreach (DashboardActionButton button in new[] { _addButton, _roleButton, _practiceButton, _removeButton })
        {
            button.Margin = new Padding(0, 0, DashboardSpacing, DashboardSpacing);
            actions.Controls.Add(button);
        }
        actions.SetFlowBreak(_roleButton, true);
        _addButton.Click += async (_, _) => await RunOperationAsync(AddUsersAsync, "Invio invito");
        _roleButton.Click += async (_, _) => await RunOperationAsync(ChangeRoleAsync, "Modifica ruolo");
        _practiceButton.Click += async (_, _) => await RunOperationAsync(EditPracticeAsync, "Modifica condivisione");
        _removeButton.Click += async (_, _) => await RunOperationAsync(RemoveUserAsync, "Revoca accesso");
        card.Controls.Add(header);
        card.Controls.Add(_participants);
        card.Controls.Add(actions);
        void LayoutParticipantsCard()
        {
            ApplyParticipantsNativeColors(surface);
            actions.Top = Math.Max(_participants.Top + DashboardSpacing, card.ClientSize.Height - actions.Height);
            actions.Width = Math.Max(200, card.ClientSize.Width);
            _participants.Size = new Size(
                Math.Max(200, card.ClientSize.Width - DashboardPadding * 2),
                Math.Max(100, actions.Top - _participants.Top));
            int columnsWidth = Math.Max(300, _participants.ClientSize.Width - 4);
            _participants.Columns[0].Width = (int)Math.Round(columnsWidth * 0.34);
            _participants.Columns[1].Width = (int)Math.Round(columnsWidth * 0.48);
            _participants.Columns[2].Width = Math.Max(80, columnsWidth - _participants.Columns[0].Width - _participants.Columns[1].Width);
        }
        card.Resize += (_, _) => LayoutParticipantsCard();
        LayoutParticipantsCard();
        return card;
    }

    private void ConfigureParticipantsContextMenu()
    {
        var menu = new ContextMenuStrip
        {
            Font = AppTypography.Create(10F),
            ShowImageMargin = false,
            BackColor = Color.White,
            Renderer = new InstitutionalContextMenuRenderer(_palette.Primary)
        };
        var editRole = new ToolStripMenuItem("Modifica ruolo");
        var removeAccess = new ToolStripMenuItem("Rimuovi accesso");
        menu.Items.Add(editRole);
        menu.Items.Add(removeAccess);

        _participants.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            ListViewItem? item = _participants.GetItemAt(e.X, e.Y);
            if (item is null)
            {
                _participants.SelectedItems.Clear();
                return;
            }
            _participants.SelectedItems.Clear();
            item.Selected = true;
            item.Focused = true;
            _participants.Focus();
        };
        _participants.MouseDoubleClick += async (_, e) =>
        {
            if (e.Button != MouseButtons.Left || _participants.SelectedItems.Count == 0) return;
            await RunOperationAsync(ChangeRoleAsync, "Modifica ruolo");
        };
        menu.Opening += (_, e) =>
        {
            bool hasSelection = _participants.SelectedItems.Count > 0;
            e.Cancel = !hasSelection;
            editRole.Enabled = hasSelection && _roleButton.Enabled;
            removeAccess.Enabled = hasSelection && _removeButton.Enabled;
        };
        editRole.Click += async (_, _) =>
            await RunOperationAsync(ChangeRoleAsync, "Modifica ruolo");
        removeAccess.Click += async (_, _) =>
            await RunOperationAsync(RemoveUserAsync, "Revoca accesso");

        _participants.ContextMenuStrip = menu;
        _participants.Disposed += (_, _) => menu.Dispose();
    }

    private void ApplyParticipantsNativeColors(Color background)
    {
        if (!_participants.IsHandleCreated) return;
        _participants.BackColor = background;
        int colorReference = ColorTranslator.ToWin32(background);
        SendMessage(_participants.Handle, 0x1001, IntPtr.Zero, (IntPtr)colorReference);
        SendMessage(_participants.Handle, 0x1026, IntPtr.Zero, (IntPtr)colorReference);
        _participants.Invalidate();
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(
        IntPtr windowHandle,
        int message,
        IntPtr parameter,
        IntPtr value);

    private WinFormsPanel CreatePracticeDetailsCard()
    {
        Color surface = _palette.IsDark ? _palette.SurfaceDark : Color.White;
        Color separator = _palette.IsDark ? Color.FromArgb(78, 78, 78) : Color.FromArgb(205, 207, 210);
        var card = new WorkspaceSectionPanel(surface, separator)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            BackColor = surface
        };
        WinFormsPanel header = CreateWorkspaceHeader("Dettagli condivisione", 13F);
        Label detailsTitle = (Label)header.Controls[0];
        var detailsTitleFont = AppTypography.Create(13F, FontStyle.Bold);
        detailsTitle.Font = detailsTitleFont;
        _details.Location = new Point(DashboardPadding + 1, 50);
        _details.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _details.Size = new Size(275, 430);
        _closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _sendLinkButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _closeButton.Click += async (_, _) => await RunOperationAsync(ClosePracticeAsync, "Revoca accessi condivisione");
        _sendLinkButton.Click += async (_, _) => await RunOperationAsync(SendPracticeLinkAsync, "Invio link condivisione");
        var actionsBand = new WorkspaceActionPanel(surface)
        {
            BackColor = surface,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        card.Controls.Add(header);
        card.Controls.Add(_details);
        card.Controls.Add(actionsBand);
        actionsBand.Controls.Add(_sendLinkButton);
        actionsBand.Controls.Add(_closeButton);
        void LayoutDetailsCard()
        {
            if (Math.Abs(detailsTitle.Font.Size - detailsTitleFont.Size) > 0.1F
                || detailsTitle.Font.Style != FontStyle.Bold)
                detailsTitle.Font = detailsTitleFont;
            _details.BackColor = card.BackColor;
            int actionsHeight = DashboardButtonHeight * 2 + DashboardSpacing + 10;
            actionsBand.SetBounds(
                1,
                Math.Max(_details.Top + DashboardSpacing, card.ClientSize.Height - actionsHeight),
                Math.Max(0, card.ClientSize.Width - 1),
                actionsHeight);
            _sendLinkButton.Top = 5;
            _closeButton.Top = _sendLinkButton.Bottom + DashboardSpacing;
            _sendLinkButton.Left = DashboardPadding;
            _closeButton.Left = DashboardPadding;
            _details.Size = new Size(
                Math.Max(180, card.ClientSize.Width - DashboardPadding * 2 - 1),
                Math.Max(100, actionsBand.Top - _details.Top));
        }
        card.Resize += (_, _) => LayoutDetailsCard();
        LayoutDetailsCard();
        return card;
    }

    private WinFormsPanel CreateWorkspaceHeader(string text, float fontSize)
    {
        Color surface = _palette.IsDark ? _palette.SurfaceDark : Color.White;
        var header = new WinFormsPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Blend(surface, _palette.Primary, _palette.IsDark ? 0.16F : 0.06F)
        };
        var title = new FixedTitleLabel(text, fontSize, _palette.Primary)
        {
            Location = new Point(DashboardPadding, 0),
            AutoSize = false,
            Height = 43,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        var separator = new WinFormsPanel
        {
            Dock = DockStyle.Bottom,
            Height = 1,
            BackColor = _palette.IsDark ? Color.FromArgb(78, 78, 78) : Color.FromArgb(222, 223, 225)
        };
        header.Controls.Add(title);
        header.Controls.Add(separator);
        header.Resize += (_, _) => title.Width = Math.Max(0, header.ClientSize.Width - DashboardPadding * 2);
        return header;
    }

    private static Color Blend(Color background, Color foreground, float foregroundAmount)
    {
        float amount = Math.Clamp(foregroundAmount, 0F, 1F);
        return Color.FromArgb(
            (int)Math.Round(background.R + (foreground.R - background.R) * amount),
            (int)Math.Round(background.G + (foreground.G - background.G) * amount),
            (int)Math.Round(background.B + (foreground.B - background.B) * amount));
    }

    private Control BuildStatusBar()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            Padding = new Padding(4, 7, 0, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent
        };

        _connectionBadge.Size = new Size(16, 16);
        _connectionBadge.Margin = new Padding(0, 0, 8, 0);
        footer.Controls.Add(_connectionBadge);

        Label[] labels = { _connectionStatus, _userStatus, _sharingStatus, _lastUpdateStatus };
        for (int index = 0; index < labels.Length; index++)
        {
            Label label = labels[index];
            label.AutoSize = true;
            label.Margin = new Padding(0, index == 3 ? 8 : 2, 0, 0);
            label.Padding = new Padding(0);
            label.TextAlign = ContentAlignment.MiddleLeft;
            footer.Controls.Add(label);
            if (index < labels.Length - 1)
            {
                var separator = new Label
                {
                    Text = "│",
                    AutoSize = true,
                    ForeColor = _palette.IsDark ? _palette.SecondaryTextDark : _palette.SecondaryTextLight,
                    BackColor = Color.Transparent,
                    Font = AppTypography.Create(9F),
                    Margin = new Padding(12, 2, 12, 0)
                };
                footer.Controls.Add(separator);
                if (index == 2) footer.SetFlowBreak(separator, true);
            }
        }

        return footer;
    }

    private void StabilizeInitialLayout()
    {
        PerformLayout();
        _root.PerformLayout();
        _root.Invalidate(true);
    }

    private async Task ConnectOnStartupAsync()
    {
        if (_connectionAttempted) return;
        _connectionAttempted = true;
        await RunOperationAsync(ConnectAsync);
    }

    public async Task InitializeFromSplashAsync(
        ShapedStartupSplashForm progress,
        int initializationMaximum = 100)
    {
        if (_connectionAttempted) return;
        _connectionAttempted = true;
        await RunOperationAsync(() => ConnectAsync(
            progress,
            closeProgress: false,
            initializationMaximum));
    }

    internal void PrepareForDisplay()
    {
        _skipExitConfirmation = false;
        DialogResult = DialogResult.None;
    }

    private async Task CheckStartupExpirationsAfterDisplayAsync()
    {
        if (_startupExpirationCheckCompleted || _currentUser is null)
            return;

        _startupExpirationCheckCompleted = true;
        await RunOperationAsync(
            () => CheckExpiredPracticesAsync(),
            "Controllo automatico scadenze");
    }

    private async Task ConnectAsync()
    {
        using var progress = new ConnectionProgressForm(
            _palette.Primary,
            _palette.PrimaryLight,
            _palette.InstitutionalAccent);
        progress.Show(this);
        await ConnectAsync(progress, closeProgress: true);
    }

    private async Task ConnectAsync(
        ShapedStartupSplashForm progress,
        bool closeProgress,
        int initializationMaximum = 100)
    {
        void Report(int percentage, string message) =>
            progress.ReportInitialization(
                Math.Clamp(
                    (int)Math.Round(percentage * initializationMaximum / 100D),
                    0,
                    initializationMaximum),
                message);

        Report(5, "Verifica della sessione Microsoft 365…");
        SetStatus("Connessione a Microsoft 365 in corso…");
        SetConnectionIndicator(ConnectionState.Connecting);
        try
        {
            AuthenticationService.CurrentUserProfile user = _authorizationSession.User
                ?? throw new InvalidOperationException("La sessione applicativa non è stata autorizzata.");
            Report(12, "Sessione utente autorizzata…");

            Report(22, "Preparazione dei registri di conformità…");
            await _sharePointService.EnsureComplianceListsAsync();
            Report(34, "Verifica della presa visione…");
            await EnsureDisclaimerAsync(user);
            _currentUser = user;

            Report(46, "Verifica delle autorizzazioni applicative…");
            _canViewAudit = await _authenticationService.IsCurrentUserMemberOfGroupAsync(_security.AuditReadersGroupId);

            Report(58, "Connessione alla raccolta documenti…");
            DocumentLibrary library = await _sharePointService.GetDefaultDocumentLibraryAsync();
            _siteName.Text = library.Name;
            _siteStatus.Text = "Servizi Microsoft 365 connessi correttamente";
            _userName.Text = user.DisplayName;
            _userEmail.Text = user.Email;
            _userStatus.Text = $"Utente: {user.DisplayName}";
            SetConnectionIndicator(ConnectionState.Connected);
            SetConnectedState(true);
            Report(72, "Caricamento delle condivisioni…");
            await LoadFoldersAsync();
            Report(88, "Finalizzazione della Data Room…");
            await RecordAuditAsync(
                "Accesso applicazione",
                "Successo",
                $"Autenticazione completata tramite {(_authenticationService.LastSignInWasSilent ? "cache protetta" : "accesso interattivo")}.");
            Report(100, "Data Room pronta.");
            SetStatus($"Connesso come {user.DisplayName} • condivisioni caricate: {_folders.Count}");
        }
        catch (Microsoft.Identity.Client.MsalClientException ex)
            when (ex.ErrorCode is "authentication_canceled" or "access_denied")
        {
            SetConnectedState(false);
            SetStatus("Accesso annullato. La nuova interfaccia rimane disconnessa.");
        }
        catch (Exception ex)
        {
            SetConnectedState(false);
            SetStatus($"Connessione non riuscita: {ex.Message}");
            AppMessageBox.Show(ex.Message, "Errore di connessione", MessageBoxButtons.OK, MessageBoxIcon.Error, this);
        }
        finally
        {
            if (closeProgress && !progress.IsDisposed) progress.Close();
            if (!IsDisposed)
            {
                Opacity = 1D;
                Activate();
                _sharingStatus.Text = $"Condivisioni caricate: {_folders.Count}";
                _lastUpdateStatus.Text = $"Ultimo aggiornamento: {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
        }
    }

    private void ShowApplicationAccessDenied(
        ConnectionProgressForm progress,
        string accountEmail,
        bool configurationMissing)
    {
        SetConnectedState(false);
        SetStatus(configurationMissing
            ? "Configurazione del gruppo utilizzatori mancante."
            : "Accesso negato: account non incluso nel gruppo utilizzatori.");
        if (!progress.IsDisposed) progress.Close();

        using var denied = new ApplicationAccessDeniedForm(accountEmail, configurationMissing);
        denied.ShowDialog(this);
        _skipExitConfirmation = true;
        Close();
    }

    private void ConfirmApplicationExit(object? sender, FormClosingEventArgs e)
    {
        if (_skipExitConfirmation || e.CloseReason != CloseReason.UserClosing) return;

        DialogResult result = AppMessageBox.Show(
            "Confermi di voler chiudere l’applicazione?",
            "Conferma uscita",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            this);
        if (result != DialogResult.Yes)
            e.Cancel = true;
        else
            ExitApplicationRequested = true;
    }

    private async Task EnsureDisclaimerAsync(AuthenticationService.CurrentUserProfile user)
    {
        if (await _sharePointService.HasDisclaimerAcceptanceAsync(user.Id, AppConstants.DisclaimerVersion)) return;
        using var dialog = new AboutForm(_branding, acknowledgementRequired: true);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            throw new OperationCanceledException("È necessario prendere visione delle condizioni d'uso.");

        string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(AppConstants.DisclaimerText)));
        await _sharePointService.RecordDisclaimerAcceptanceAsync(user, AppConstants.DisclaimerVersion, hash);
    }

    private async Task LoadFoldersAsync()
    {
        IReadOnlyList<SharePointService.DriveItem> folders = await _sharePointService.GetFoldersAsync();
        _folders.Clear();
        foreach (SharePointService.DriveItem folder in folders) _folders[folder.Id] = folder;
        foreach (string removedId in _folderStatuses.Keys.Where(id => !_folders.ContainsKey(id)).ToList())
            _folderStatuses.Remove(removedId);
        _sharingStatus.Text = $"Condivisioni caricate: {_folders.Count}";
        _lastUpdateStatus.Text = $"Ultimo aggiornamento: {DateTime.Now:dd/MM/yyyy HH:mm}";
    }

    private async Task EnsureMissingFolderStatusesAsync()
    {
        foreach (SharePointService.DriveItem folder in _folders.Values.Where(folder => !_folderStatuses.ContainsKey(folder.Id)))
        {
            try
            {
                SharePointService.FolderMetadata metadata = await _sharePointService.GetFolderMetadataAsync(folder.Id);
                _folderStatuses[folder.Id] = await BuildExpirationLabelAsync(folder.Id, metadata.ExpirationDate);
            }
            catch (Exception ex)
            {
                _folderStatuses[folder.Id] = "Da verificare";
                UiLogger.Info($"[ERRORE] Controllo scadenza non riuscito per '{folder.Name}': {ex.Message}");
            }
        }
    }

    private async Task<string> BuildExpirationLabelAsync(string folderId, DateTimeOffset? expiration)
    {
        if (expiration is null) return "Nessuna scadenza";
        string label = expiration.Value.LocalDateTime.ToString("dd/MM/yyyy");
        if (expiration.Value.Date < DateTimeOffset.Now.Date)
            return await _sharePointService.HasClosablePermissionsAsync(folderId, InternalUserDomain())
                ? $"{label} — Scaduta"
                : $"{label} — Chiusa";

        int remainingDays = (expiration.Value.Date - DateTimeOffset.Now.Date).Days;
        return remainingDays <= AppConstants.ExpirationWarningDays
            ? $"{label} — tra {remainingDays} g"
            : label;
    }

    private async Task CheckExpirationsManuallyAsync()
    {
        SetStatus("Aggiornamento e controllo delle scadenze in corsoâ€¦");
        await LoadFoldersAsync();
        await CheckExpiredPracticesAsync(notifyWhenNone: true);
        SetStatus($"Controllo scadenze completato â€¢ condivisioni analizzate: {_folders.Count}");
    }

    private async Task CheckExpiredPracticesAsync(
        Action<int, int>? reportProgress = null,
        bool notifyWhenNone = false)
    {
        SharePointService.DriveItem[] folders = _folders.Values.ToArray();
        var expiredPractices = new System.Collections.Concurrent.ConcurrentBag<ExpiredPracticeInfo>();
        int processed = 0;
        int total = folders.Length;
        reportProgress?.Invoke(0, total);

        using var limiter = new SemaphoreSlim(Math.Min(6, Math.Max(1, total)));
        await Task.WhenAll(folders.Select(async folder =>
        {
            await limiter.WaitAsync();
            try
            {
                SharePointService.FolderMetadata metadata = await _sharePointService.GetFolderMetadataAsync(folder.Id);
                DateTimeOffset? expiration = metadata.ExpirationDate;
                if (expiration is null)
                {
                    _folderStatuses[folder.Id] = "Nessuna scadenza";
                    return;
                }

                string expirationLabel = expiration.Value.LocalDateTime.ToString("dd/MM/yyyy");
                if (expiration.Value.Date < DateTimeOffset.Now.Date)
                {
                    bool hasAccesses = await _sharePointService.HasClosablePermissionsAsync(folder.Id, InternalUserDomain());
                    _folderStatuses[folder.Id] = hasAccesses
                        ? $"{expirationLabel} — Scaduta"
                        : $"{expirationLabel} — Chiusa";
                    if (hasAccesses)
                        expiredPractices.Add(new ExpiredPracticeInfo(folder.Id, folder.Name, expiration.Value));
                    return;
                }

                int remainingDays = (expiration.Value.Date - DateTimeOffset.Now.Date).Days;
                _folderStatuses[folder.Id] = remainingDays <= AppConstants.ExpirationWarningDays
                    ? $"{expirationLabel} — tra {remainingDays} g"
                    : expirationLabel;
            }
            catch (Exception ex)
            {
                _folderStatuses[folder.Id] = "Da verificare";
                UiLogger.Info($"[ERRORE] Controllo scadenza non riuscito per '{folder.Name}': {ex.Message}");
            }
            finally
            {
                limiter.Release();
                int completed = Interlocked.Increment(ref processed);
                reportProgress?.Invoke(completed, total);
            }
        }));

        if (expiredPractices.Count == 0)
        {
            if (notifyWhenNone)
            {
                AppMessageBox.Show(
                    "Non sono state rilevate condivisioni scadute con accessi esterni ancora attivi.",
                    "Controllo scadenze",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    this);
            }
            return;
        }
        using var dialog = new ExpiredPracticesForm(
            expiredPractices.OrderBy(item => item.ExpirationDate).ToArray());
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!string.IsNullOrWhiteSpace(dialog.PracticeIdToOpen))
        {
            await SelectPracticeAsync(dialog.PracticeIdToOpen);
            return;
        }
        if (dialog.PracticeIdsToClose.Count == 0) return;

        int count = dialog.PracticeIdsToClose.Count;
        DialogResult confirmation = AppMessageBox.Show(
            $"Confermi la chiusura di {(count == 1 ? "1 condivisione" : $"{count} condivisioni")}?\n\nSaranno revocati esclusivamente gli accessi esterni. Gli utenti interni resteranno autorizzati.",
            "Conferma chiusura",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            this);
        if (confirmation != DialogResult.Yes) return;

        foreach (string practiceId in dialog.PracticeIdsToClose)
        {
            string practiceName = _folders.GetValueOrDefault(practiceId)?.Name ?? practiceId;
            try
            {
                SharePointService.PracticeClosureResult result = await _sharePointService.ClosePracticeAsync(practiceId, InternalUserDomain());
                foreach (string email in result.RevokedUsers)
                    await RecordAuditAsync(
                        "Chiusura condivisione",
                        "Successo",
                        "Accesso esterno revocato per scadenza.",
                        practiceId,
                        practiceName,
                        email);
                foreach (string error in result.Errors)
                    await RecordAuditAsync("Chiusura condivisione", "Errore", error, practiceId, practiceName);
                if (result.Errors.Count == 0) _folderStatuses[practiceId] = "Chiusa";
            }
            catch (Exception ex)
            {
                await RecordAuditAsync("Chiusura condivisione", "Errore", ex.Message, practiceId, practiceName);
            }
        }
    }

    private async Task OpenPracticeAsync()
    {
        using var dialog = new CondivisioneSelectorForm();

        async Task ReloadDialogAsync()
        {
            await LoadFoldersAsync();
            await EnsureMissingFolderStatusesAsync();
            SharePointService.DriveItem[] folders =
                _folders.Values.OrderBy(folder => folder.Name).ToArray();
            var rows = await Task.WhenAll(folders.Select(async folder =>
            {
                try
                {
                    SharePointService.FolderMetadata metadata =
                        await _sharePointService.GetFolderMetadataAsync(folder.Id);
                    return (Folder: folder, Metadata: metadata);
                }
                catch (Exception ex)
                {
                    UiLogger.Info($"[ERRORE] Lettura dettagli di '{folder.Name}' non riuscita: {ex.Message}");
                    return (
                        Folder: folder,
                        Metadata: new SharePointService.FolderMetadata(string.Empty, string.Empty, null));
                }
            }));

            dialog.ClearCondivisioni();
            foreach (var row in rows)
                dialog.AddCondivisione(
                    row.Folder.Name,
                    row.Folder.CreatedDateTime?.LocalDateTime.ToString("dd/MM/yyyy HH:mm") ?? "-",
                    _folderStatuses.GetValueOrDefault(row.Folder.Id, "Da verificare"),
                    row.Folder.Id,
                    row.Metadata.RequestedBy,
                    row.Metadata.Notes);
        }

        dialog.RefreshRequested = ReloadDialogAsync;
        await ReloadDialogAsync();
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await SelectPracticeAsync(dialog.SelectedItemId);
    }

    private async Task CreatePracticeAsync()
    {
        using var dialog = new PracticeMetadataForm();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string finalName = await _sharePointService.CreateFolderAsync(dialog.PracticeName);
        await LoadFoldersAsync();
        SharePointService.DriveItem folder = _folders.Values.First(item => item.Name.Equals(finalName, StringComparison.OrdinalIgnoreCase));
        await _sharePointService.UpdateFolderMetadataAsync(folder.Id, dialog.RequestedBy, dialog.Notes, dialog.ExpirationDate);
        _folderStatuses[folder.Id] = await BuildExpirationLabelAsync(folder.Id, dialog.ExpirationDate);
        await RecordAuditAsync(
            "Creazione condivisione",
            "Successo",
            $"Condivisione creata. Richiesto da: {dialog.RequestedBy}. Scadenza: {FormatExpiration(dialog.ExpirationDate)}.",
            folder.Id,
            folder.Name);
        await SendCreationNotificationSilentlyAsync(folder, dialog);
        await SelectPracticeAsync(folder.Id);
    }

    private async Task SendCreationNotificationSilentlyAsync(
        SharePointService.DriveItem folder,
        PracticeMetadataForm dialog)
    {
        const string recipient = "dataroom_support@giovanardilex.it";
        try
        {
            if (_currentUser is null)
                throw new InvalidOperationException("Utente connesso non disponibile.");

            await _sharePointService.SendCreationNotificationEmailAsync(
                _currentUser,
                folder.Name,
                dialog.RequestedBy,
                dialog.Notes,
                dialog.ExpirationDate,
                folder.CreatedDateTime ?? DateTimeOffset.Now,
                folder.WebUrl);
            await RecordAuditAsync(
                "Notifica creazione condivisione",
                "Successo",
                "Notifica automatica inviata al supporto Data Room.",
                folder.Id,
                folder.Name,
                recipient);
        }
        catch (Exception ex)
        {
            UiLogger.Info($"[ERRORE] Notifica automatica di creazione non inviata: {ex.Message}");
            await RecordAuditAsync(
                "Notifica creazione condivisione",
                "Errore",
                ex.Message,
                folder.Id,
                folder.Name,
                recipient);
        }
    }

    private async Task SelectPracticeAsync(string practiceId)
    {
        if (!_folders.ContainsKey(practiceId)) return;
        _selectedPracticeId = practiceId;
        await Task.WhenAll(LoadDetailsAsync(), LoadPermissionsAsync());
        SetPracticeState(true);
        await RecordAuditAsync("Apertura condivisione", "Successo", "Condivisione selezionata e dati caricati.");
    }

    private async Task LoadDetailsAsync()
    {
        SharePointService.DriveItem folder = CurrentFolder();
        SharePointService.FolderMetadata metadata = await _sharePointService.GetFolderMetadataAsync(folder.Id);
        Color headingColor = _palette.IsDark ? _palette.TextDark : _palette.TextLight;
        Color valueColor = _palette.IsDark ? _palette.SecondaryTextDark : _palette.SecondaryTextLight;
        var rtf = new StringBuilder();
        rtf.Append(@"{\rtf1\ansi\deff0")
            .Append($@"{{\fonttbl{{\f0 {AppTypography.FamilyName};}}}}")
            .Append(@"{\colortbl ;")
            .Append(RtfColor(headingColor))
            .Append(RtfColor(valueColor))
            .Append(@"}\viewkind4\uc1\pard\f0\fs20\b\cf1 Nome condivisione\b0\par\cf2 ")
            .Append(EscapeRtf(folder.Name));

        AppendRtfSection(rtf, "Richiesto da", EmptyValue(metadata.RequestedBy));
        AppendRtfSection(rtf, "Note", EmptyValue(metadata.Notes));
        AppendRtfSection(rtf, "Scadenza", FormatExpiration(metadata.ExpirationDate));
        AppendRtfSection(rtf, "Creata il", folder.CreatedDateTime?.LocalDateTime.ToString("dd/MM/yyyy HH:mm") ?? "-");
        AppendRtfSection(rtf, "Creata da", folder.CreatedBy?.User?.DisplayName ?? "-");
        rtf.Append('}');
        _details.Rtf = rtf.ToString();
    }

    private static void AppendRtfSection(StringBuilder rtf, string label, string value)
    {
        rtf.Append(@"\par\par\b\cf1 ")
            .Append(EscapeRtf(label))
            .Append(@"\b0\par\cf2 ")
            .Append(EscapeRtf(value));
    }

    private static string RtfColor(Color color) =>
        $@"\red{color.R}\green{color.G}\blue{color.B};";

    private static string EscapeRtf(string value)
    {
        var escaped = new StringBuilder(value.Length + 16);
        foreach (char character in value)
        {
            switch (character)
            {
                case '\\': escaped.Append(@"\\"); break;
                case '{': escaped.Append(@"\{"); break;
                case '}': escaped.Append(@"\}"); break;
                case '\r': break;
                case '\n': escaped.Append(@"\line "); break;
                default:
                    if (character <= 0x7f)
                        escaped.Append(character);
                    else
                        escaped.Append(@"\u").Append(unchecked((short)character)).Append('?');
                    break;
            }
        }
        return escaped.ToString();
    }

    private async Task LoadPermissionsAsync()
    {
        List<PermissionEntry> permissions = await _sharePointService.GetPermissionsAsync(_selectedPracticeId);
        _participants.BeginUpdate();
        _participants.Items.Clear();
        foreach (PermissionEntry permission in permissions.Where(permission =>
                     !permission.Role.Equals("owner", StringComparison.OrdinalIgnoreCase)
                     && IsValidEmail(permission.Email)))
        {
            bool isExternal = permission.IsExternal || IsExternalAddress(permission.Email);
            var item = new ListViewItem(permission.DisplayName)
            {
                ImageKey = isExternal ? "external" : "internal",
                ToolTipText = isExternal
                    ? "Indirizzo esterno al dominio dello Studio"
                    : "Indirizzo interno al dominio dello Studio"
            };
            item.SubItems.Add(permission.Email);
            item.SubItems.Add(RoleLabel(permission.Role));
            if (isExternal)
            {
                item.UseItemStyleForSubItems = false;
                item.BackColor = BlendWithWhite(_palette.Warning, 0.90F);
                item.SubItems[1].BackColor = item.BackColor;
                item.SubItems[2].BackColor = item.BackColor;
                item.SubItems[1].ForeColor = Color.FromArgb(145, 82, 0);
            }
            item.Tag = permission.Id;
            _participants.Items.Add(item);
        }
        _participants.EndUpdate();
        ApplyParticipantsNativeColors(_palette.IsDark ? _palette.SurfaceDark : Color.White);
    }

    private async Task AddUsersAsync()
    {
        using var dialog = new InviteModalForm(
            CurrentFolder().Name,
            _authenticationService,
            InternalUserDomain());
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        int successes = 0;
        var errors = new List<string>();
        SharePointService.DriveItem folder = CurrentFolder();
        foreach (InviteRecipient invitation in dialog.Invitations)
        {
            try
            {
                await _sharePointService.AddPermissionAsync(
                    _selectedPracticeId,
                    invitation.Email,
                    invitation.Role == "Scrittura" ? "write" : "read",
                    IsExternalAddress(invitation.Email),
                    false,
                    dialog.Message);
                await _sharePointService.SendPracticeLinkEmailAsync(
                    invitation.Email,
                    folder.Name,
                    folder.WebUrl,
                    dialog.Message);
                successes++;
                await RecordAuditAsync(
                    "Invio invito",
                    "Successo",
                    $"Permesso {invitation.Role} assegnato e invito inviato dalla casella condivisa.",
                    recipient: invitation.Email);
            }
            catch (Exception ex)
            {
                errors.Add($"{invitation.Email}: {ex.Message}");
                await RecordAuditAsync("Invio invito", "Errore", ex.Message, recipient: invitation.Email);
            }
        }
        await LoadPermissionsAsync();
        AppMessageBox.Show(
            errors.Count == 0
                ? $"Inviti inviati correttamente: {successes}."
                : $"Inviti inviati: {successes}. Non riusciti: {errors.Count}.\n\n{string.Join("\n", errors)}",
            errors.Count == 0 ? "Inviti completati" : "Inviti completati con errori",
            MessageBoxButtons.OK,
            errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning,
            this);
    }

    private async Task ChangeRoleAsync()
    {
        ListViewItem item = SelectedParticipant();
        string permissionId = item.Tag?.ToString() ?? throw new InvalidOperationException("Permesso non valido.");
        string currentRole = item.SubItems[2].Text.Equals("Scrittura", StringComparison.OrdinalIgnoreCase) ? "write" : "read";
        using var dialog = new RoleSelectionForm(item.Text, currentRole);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedRole == currentRole) return;

        await _sharePointService.UpdatePermissionRoleAsync(_selectedPracticeId, permissionId, dialog.SelectedRole);
        await RecordAuditAsync(
            "Modifica ruolo",
            "Successo",
            $"Ruolo modificato da {RoleLabel(currentRole)} a {RoleLabel(dialog.SelectedRole)}.",
            recipient: item.SubItems[1].Text);
        await LoadPermissionsAsync();
    }

    private async Task EditPracticeAsync()
    {
        SharePointService.DriveItem folder = CurrentFolder();
        SharePointService.FolderMetadata metadata = await _sharePointService.GetFolderMetadataAsync(folder.Id);
        using var dialog = new PracticeMetadataForm(folder.Name, metadata);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        string originalName = folder.Name;
        string updatedName = originalName;
        if (!originalName.Equals(dialog.PracticeName, StringComparison.Ordinal))
        {
            updatedName = await _sharePointService.RenameFolderAsync(folder.Id, dialog.PracticeName);
            await LoadFoldersAsync();
        }
        await _sharePointService.UpdateFolderMetadataAsync(folder.Id, dialog.RequestedBy, dialog.Notes, dialog.ExpirationDate);
        _folderStatuses[folder.Id] = await BuildExpirationLabelAsync(folder.Id, dialog.ExpirationDate);
        var changes = new List<string>();
        if (!originalName.Equals(updatedName, StringComparison.Ordinal))
            changes.Add($"Nome modificato da '{originalName}' a '{updatedName}'.");
        if (!metadata.RequestedBy.Equals(dialog.RequestedBy, StringComparison.Ordinal)) changes.Add("Richiedente modificato.");
        if (!metadata.Notes.Equals(dialog.Notes, StringComparison.Ordinal)) changes.Add("Note modificate.");
        if (metadata.ExpirationDate?.Date != dialog.ExpirationDate?.Date)
            changes.Add($"Scadenza modificata da {FormatExpiration(metadata.ExpirationDate)} a {FormatExpiration(dialog.ExpirationDate)}.");
        await LoadDetailsAsync();
        await RecordAuditAsync("Modifica condivisione", "Successo", changes.Count == 0 ? "Salvataggio senza variazioni." : string.Join(" ", changes));
    }

    private async Task RemoveUserAsync()
    {
        ListViewItem item = SelectedParticipant();
        string permissionId = item.Tag?.ToString() ?? throw new InvalidOperationException("Permesso non valido.");
        string email = item.SubItems[1].Text;
        DialogResult confirmation = AppMessageBox.Show(
            $"Confermi la revoca dell'accesso?\n\nUtente: {item.Text}\nEmail: {email}\nRuolo: {item.SubItems[2].Text}\nCondivisione: {CurrentFolder().Name}",
            "Conferma revoca accesso",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            this);
        if (confirmation != DialogResult.Yes) return;

        await _sharePointService.RemovePermissionAsync(_selectedPracticeId, permissionId);
        await RecordAuditAsync("Revoca accesso", "Successo", $"Rimosso il permesso {item.SubItems[2].Text}.", recipient: email);
        await LoadPermissionsAsync();
    }

    private async Task ClosePracticeAsync()
    {
        DialogResult confirmation = AppMessageBox.Show(
            $"Confermi la revoca di tutti gli accessi esterni alla condivisione '{CurrentFolder().Name}'?\n\nGli utenti interni, la cartella e i documenti non verranno modificati.",
            "Conferma chiusura condivisione",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            this);
        if (confirmation != DialogResult.Yes) return;

        SharePointService.PracticeClosureResult result = await _sharePointService.ClosePracticeAsync(_selectedPracticeId, InternalUserDomain());
        foreach (string email in result.RevokedUsers)
            await RecordAuditAsync("Revoca accessi condivisione", "Successo", "Accesso esterno revocato manualmente.", recipient: email);
        foreach (string error in result.Errors)
            await RecordAuditAsync("Revoca accessi condivisione", "Errore", error);
        await LoadPermissionsAsync();
        AppMessageBox.Show(
            result.Errors.Count == 0
                ? $"Accessi revocati: {result.RevokedUsers.Count}."
                : $"Revocati: {result.RevokedUsers.Count}; errori: {result.Errors.Count}.",
            "Chiusura condivisione",
            MessageBoxButtons.OK,
            result.Errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning,
            this);
    }

    private async Task SendPracticeLinkAsync()
    {
        SharePointService.DriveItem folder = CurrentFolder();
        List<PermissionEntry> permissions = await _sharePointService.GetPermissionsAsync(folder.Id);
        List<string> authorizedEmails = permissions
            .Where(permission => IsValidEmail(permission.Email)
                && !permission.Role.Equals("owner", StringComparison.OrdinalIgnoreCase))
            .Select(permission => permission.Email)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(email => email, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (authorizedEmails.Count == 0)
        {
            AppMessageBox.Show(
                "La condivisione non contiene destinatari autorizzati ai quali inviare il collegamento.",
                "Nessun destinatario",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning,
                this);
            return;
        }

        using var dialog = new SendPracticeLinkForm(folder.Name, authorizedEmails);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        int successes = 0;
        var errors = new List<string>();
        foreach (string email in dialog.SelectedEmails)
        {
            try
            {
                await _sharePointService.SendPracticeLinkEmailAsync(email, folder.Name, folder.WebUrl);
                successes++;
                await RecordAuditAsync(
                    "Invio link condivisione",
                    "Successo",
                    "Collegamento alla condivisione inviato tramite Microsoft 365.",
                    recipient: email);
            }
            catch (Exception ex)
            {
                errors.Add($"{email}: {ex.Message}");
                await RecordAuditAsync("Invio link condivisione", "Errore", ex.Message, recipient: email);
            }
        }

        AppMessageBox.Show(
            errors.Count == 0
                ? $"Collegamento inviato correttamente a {successes} destinatari."
                : $"Invii riusciti: {successes}. Non riusciti: {errors.Count}.\n\n{string.Join("\n", errors)}",
            errors.Count == 0 ? "Link inviati" : "Invio completato con errori",
            MessageBoxButtons.OK,
            errors.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning,
            this);
    }

    private async Task RecordAuditAsync(
        string operation,
        string outcome,
        string details,
        string? practiceId = null,
        string? practiceName = null,
        string? recipient = null)
    {
        if (_currentUser is null) return;
        practiceId ??= string.IsNullOrWhiteSpace(_selectedPracticeId) ? null : _selectedPracticeId;
        practiceName ??= practiceId is null ? null : _folders.GetValueOrDefault(practiceId)?.Name;
        try
        {
            await _sharePointService.RecordActivityAsync(
                _currentUser,
                operation,
                outcome,
                practiceId,
                practiceName,
                recipient,
                details);
        }
        catch (Exception ex)
        {
            UiLogger.Info($"[ERRORE] Registrazione audit non riuscita per '{operation}': {ex.Message}");
        }
    }

    private async Task RunOperationAsync(Func<Task> operation, string? auditOperation = null)
    {
        if (_operationInProgress) return;
        _operationInProgress = true;
        UseWaitCursor = true;
        try
        {
            await operation();
        }
        catch (OperationCanceledException ex)
        {
            SetStatus(ex.Message);
        }
        catch (Exception ex)
        {
            SetStatus($"Operazione non riuscita: {ex.Message}");
            if (!string.IsNullOrWhiteSpace(auditOperation))
                await RecordAuditAsync(auditOperation, "Errore", ex.Message);
            AppMessageBox.Show(ex.Message, "Operazione non riuscita", MessageBoxButtons.OK, MessageBoxIcon.Error, this);
        }
        finally
        {
            UseWaitCursor = false;
            _operationInProgress = false;
            RestoreWorkspaceVisualState();
        }
    }

    private void OpenAuditViewer()
    {
        if (!_canViewAudit)
        {
            AppMessageBox.Show(
                "La consultazione del registro è riservata agli utenti autorizzati.",
                "Accesso riservato",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                this);
            return;
        }

        string? selectedSharingName = string.IsNullOrWhiteSpace(_selectedPracticeId)
            ? null
            : CurrentFolder().Name;
        using var dialog = new AuditViewerForm(
            _sharePointService,
            selectedSharingName,
            string.IsNullOrWhiteSpace(_selectedPracticeId) ? null : _selectedPracticeId);
        dialog.ShowDialog(this);
    }

    private void OpenLocalLogViewer()
    {
        if (!_canViewAudit) return;
        using var dialog = new LocalLogViewerForm(_themeManager);
        dialog.ShowDialog(this);
    }

    private void ResetButtonVisualStates()
    {
        if (IsDisposed) return;
        ActiveControl = null;
        foreach (DashboardActionButton button in FindControls<DashboardActionButton>(this))
            button.ResetVisualState();
        foreach (MaterialButton button in FindControls<MaterialButton>(this))
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

    private void RestoreAfterPopup()
    {
        if (IsDisposed) return;
        ResetButtonVisualStates();
        RestoreWorkspaceVisualState();
    }

    private void RestoreWorkspaceVisualState()
    {
        if (IsDisposed) return;

        // ShowDialog disabilita temporaneamente la finestra proprietaria.
        // I controlli Win32 possono conservare i colori dello stato disabilitato
        // anche dopo il ritorno da popup aperti con percorsi differenti.
        Color workspaceSurface = _palette.IsDark ? _palette.SurfaceDark : Color.White;
        ApplyParticipantsNativeColors(workspaceSurface);
        _details.LockBackColor(workspaceSurface);
        _participants.Invalidate();
        _details.Invalidate();
        _participants.Update();
        _details.Update();
    }

    private static IEnumerable<TControl> FindControls<TControl>(Control parent)
        where TControl : Control
    {
        foreach (Control child in parent.Controls)
        {
            if (child is TControl match) yield return match;
            foreach (TControl nested in FindControls<TControl>(child)) yield return nested;
        }
    }

    private void SetConnectedState(bool connected)
    {
        _actionToolbar.SuspendLayout();
        try
        {
            _createButton.Enabled = connected;
            _openButton.Enabled = connected;
            _expirationButton.Enabled = connected;
            bool showAdministrativeCommands = connected && _canViewAudit;
            UpdateAdministrativeToolbar(showAdministrativeCommands);
            if (!connected)
            {
                _siteName.Text = "-";
                _siteStatus.Text = "Servizi Microsoft 365 non connessi";
                _userName.Text = "Non autenticato";
                _userEmail.Text = "-";
                _userStatus.Text = "Utente: -";
                _sharingStatus.Text = "Condivisioni caricate: 0";
                _lastUpdateStatus.Text = "Ultimo aggiornamento: -";
                SetConnectionIndicator(ConnectionState.Disconnected);
                _canViewAudit = false;
                SetPracticeState(false);
            }
        }
        finally
        {
            _actionToolbar.ResumeLayout(performLayout: true);
            _actionToolbar.PerformLayout();
            _actionToolbar.Invalidate(invalidateChildren: true);
        }
    }

    private void UpdateAdministrativeToolbar(bool show)
    {
        _auditButton.Enabled = show;
        _localLogButton.Enabled = show;

        // I controlli ReaLTaiizor invisibili possono lasciare per un fotogramma
        // la propria ombra nel FlowLayoutPanel. Li rimuoviamo davvero dal layout.
        _actionToolbar.Controls.Remove(_localLogButton);
        _actionToolbar.Controls.Remove(_auditButton);
        if (!show) return;

        _actionToolbar.Controls.Remove(_infoButton);
        _actionToolbar.Controls.Add(_localLogButton);
        _actionToolbar.Controls.Add(_auditButton);
        _actionToolbar.Controls.Add(_infoButton);
    }

    private void SetPracticeState(bool active)
    {
        _addButton.Enabled = active;
        _roleButton.Enabled = active && _participants.SelectedItems.Count > 0;
        _practiceButton.Enabled = active;
        _removeButton.Enabled = active && _participants.SelectedItems.Count > 0;
        _closeButton.Enabled = active;
        _sendLinkButton.Enabled = active;
        if (!active)
        {
            _selectedPracticeId = string.Empty;
            _participants.Items.Clear();
            _details.Text = "Nessuna condivisione selezionata";
        }
    }

    private void UpdateParticipantSelectionState()
    {
        bool sharingSelected = !string.IsNullOrWhiteSpace(_selectedPracticeId);
        bool participantSelected = _participants.SelectedItems.Count > 0;
        _roleButton.Enabled = sharingSelected && participantSelected;
        _removeButton.Enabled = sharingSelected && participantSelected;
    }

    private SharePointService.DriveItem CurrentFolder() =>
        !string.IsNullOrWhiteSpace(_selectedPracticeId) && _folders.TryGetValue(_selectedPracticeId, out SharePointService.DriveItem? folder)
            ? folder
            : throw new InvalidOperationException("Nessuna condivisione selezionata.");

    private ListViewItem SelectedParticipant() =>
        _participants.SelectedItems.Count > 0
            ? _participants.SelectedItems[0]
            : throw new InvalidOperationException("Seleziona prima un utente dall'elenco.");

    private void SetStatus(string value)
    {
        _status.Text = value;
        UiLogger.Info(value);
    }

    private void SetConnectionIndicator(ConnectionState state)
    {
        string stateText = state switch
        {
            ConnectionState.Connected => "Microsoft 365 connesso",
            ConnectionState.Connecting => "Connessione a Microsoft 365 in corso",
            _ => "Microsoft 365 disconnesso"
        };
        _connectionBadge.AccessibleName = stateText;
        _connectionStatus.Text = stateText;
        _connectionBadge.IndicatorColor = state switch
        {
            ConnectionState.Connected => _palette.Success,
            ConnectionState.Connecting => _palette.Warning,
            _ => _palette.Danger
        };
    }

    private enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    private sealed class ConnectionStatusIndicator : Control
    {
        private Color _indicatorColor = Color.Gray;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color IndicatorColor
        {
            get => _indicatorColor;
            set
            {
                _indicatorColor = value;
                Invalidate();
            }
        }

        public ConnectionStatusIndicator()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.SupportsTransparentBackColor |
                     ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(_indicatorColor);
            e.Graphics.FillEllipse(brush, 2, 2, Math.Max(1, Width - 4), Math.Max(1, Height - 4));
        }
    }

    private sealed class MaterialRichTextDisplay : RichTextBox,
        ReaLTaiizor.Helper.MaterialDrawHelper.MaterialControlI
    {
        private Color? _lockedBackColor;

        public override Color BackColor
        {
            get => base.BackColor;
            set => base.BackColor = _lockedBackColor ?? value;
        }

        public void LockBackColor(Color color)
        {
            _lockedBackColor = color;
            base.BackColor = color;
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Depth { get; set; }

        [System.ComponentModel.Browsable(false)]
        public ReaLTaiizor.Manager.MaterialSkinManager SkinManager =>
            ReaLTaiizor.Manager.MaterialSkinManager.Instance;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(
            System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState MouseState { get; set; }
    }

    private bool IsExternalAddress(string email) =>
        _currentUser is not null
        && MailAddress.TryCreate(_currentUser.Email, out MailAddress? current)
        && MailAddress.TryCreate(email, out MailAddress? invited)
        && !current.Host.Equals(invited.Host, StringComparison.OrdinalIgnoreCase);

    private string InternalUserDomain()
    {
        if (_currentUser is not null
            && MailAddress.TryCreate(_currentUser.Email, out MailAddress? current))
            return current.Host;

        throw new InvalidOperationException("Impossibile determinare il dominio interno dell'utente connesso.");
    }

    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email, out MailAddress? address)
        && address.Address.Equals(email, StringComparison.OrdinalIgnoreCase);

    private static string RoleLabel(string role) => role.Equals("write", StringComparison.OrdinalIgnoreCase) ? "Scrittura" : "Lettura";
    private static string FormatExpiration(DateTimeOffset? value) => value?.LocalDateTime.ToString("dd/MM/yyyy") ?? "nessuna";
    private static string EmptyValue(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static MaterialCard CreateCard()
    {
        var card = new MaterialCard
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiMetrics.CardPadding),
            Margin = new Padding(0)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(42, 90, 90, 90));
            int radius = UiMetrics.CornerRadius;
            e.Graphics.DrawLine(pen, 0, radius, 0, card.ClientSize.Height - radius - 1);
            e.Graphics.DrawLine(pen, radius, 0, card.ClientSize.Width - radius - 1, 0);
            e.Graphics.DrawArc(pen, 0, 0, radius * 2, radius * 2, 180, 90);
        };
        return card;
    }

    private static Label AddCardTitle(MaterialCard card, string text, float size = 18F, int x = UiMetrics.CardPadding)
    {
        Label title = new FixedTitleLabel(text, size);
        title.Location = new Point(x, 16);
        title.AutoSize = true;
        card.Controls.Add(title);
        return title;
    }

    private sealed class FixedTitleLabel : Label
    {
        private readonly Font _displayFont;
        private readonly Color? _displayColor;

        public FixedTitleLabel(string text, float size, Color? displayColor = null)
        {
            Text = text;
            _displayFont = AppTypography.Create(size, FontStyle.Bold);
            _displayColor = displayColor;
            Font = _displayFont;
            if (displayColor.HasValue) ForeColor = displayColor.Value;
            BackColor = Color.Transparent;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
        }

        public override Size GetPreferredSize(Size proposedSize) => TextRenderer.MeasureText(
            Text,
            _displayFont,
            Size.Empty,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);

        protected override void OnPaint(PaintEventArgs e)
        {
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                _displayFont,
                ClientRectangle,
                _displayColor ?? ForeColor,
                TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _displayFont.Dispose();
            base.Dispose(disposing);
        }
    }

    private void ConfigureButtonIcons()
    {
        _createButton.Icon = CreateButtonIcon("folder-plus");
        _openButton.Icon = CreateButtonIcon("folder-open");
        _addButton.Icon = CreateButtonIcon("plus");
        _roleButton.Icon = CreateButtonIcon("edit");
        _practiceButton.Icon = CreateButtonIcon("document-edit");
        _removeButton.Icon = CreateButtonIcon("trash");
        _closeButton.Icon = CreateButtonIcon("trash");
        _sendLinkButton.Icon = CreateButtonIcon("email");
        _expirationButton.Icon = CreateButtonIcon("calendar-check");
        _localLogButton.Icon = CreateButtonIcon("log");
        _auditButton.Icon = CreateButtonIcon("audit");
        _infoButton.Icon = CreateButtonIcon("info");
    }

    private void ConfigureDashboardButtonPalette()
    {
        foreach (DashboardActionButton button in new[]
                 {
                     _createButton, _openButton, _addButton, _roleButton, _practiceButton,
                     _removeButton, _closeButton, _sendLinkButton, _localLogButton,
                     _expirationButton, _auditButton, _infoButton
                 })
        {
            button.PrimaryColor = _palette.DashboardButtonBackground;
            button.PrimaryForeColor = _palette.DashboardButtonForeground;
            button.DangerColor = _palette.Danger;
            button.DisabledBackColor = _palette.IsDark
                ? Color.FromArgb(70, 70, 70)
                : Color.FromArgb(228, 228, 228);
            button.DisabledForeColor = _palette.IsDark
                ? Color.FromArgb(145, 145, 145)
                : Color.FromArgb(145, 145, 145);
        }
        _infoButton.ExtraHorizontalPadding = 12;
        _createButton.ExtraHorizontalPadding = 10;
        _openButton.ExtraHorizontalPadding = 10;
        _addButton.ExtraHorizontalPadding = 10;
        _practiceButton.ExtraHorizontalPadding = 12;
        _sendLinkButton.ExtraHorizontalPadding = 10;
        _closeButton.ExtraHorizontalPadding = 12;
        _roleButton.IconVerticalOffset = -1;
        _sendLinkButton.AlignContentLeft = true;
        _closeButton.AlignContentLeft = true;
    }

    internal static Bitmap CreateButtonIcon(string type)
    {
        var image = new Bitmap(18, 18);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        graphics.ScaleTransform(0.75F, 0.75F);
        using var pen = new Pen(Color.White, 2F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        using var brush = new SolidBrush(Color.White);

        switch (type)
        {
            case "folder-plus":
            case "folder-open":
                graphics.DrawRectangle(pen, 3, 8, 15, 11);
                graphics.DrawLine(pen, 4, 6, 10, 6);
                graphics.DrawLine(pen, 10, 6, 12, 8);
                if (type == "folder-plus")
                {
                    graphics.DrawLine(pen, 19, 14, 19, 22);
                    graphics.DrawLine(pen, 15, 18, 23, 18);
                }
                else
                {
                    graphics.DrawLine(pen, 7, 16, 17, 12);
                }
                break;
            case "home":
                graphics.DrawLine(pen, 3, 12, 12, 4);
                graphics.DrawLine(pen, 12, 4, 21, 12);
                graphics.DrawRectangle(pen, 6, 11, 12, 10);
                break;
            case "plus":
                graphics.DrawLine(pen, 12, 5, 12, 19);
                graphics.DrawLine(pen, 5, 12, 19, 12);
                break;
            case "edit":
                graphics.DrawLine(pen, 5, 18, 16, 7);
                graphics.DrawLine(pen, 8, 21, 19, 10);
                graphics.DrawLine(pen, 16, 7, 19, 10);
                graphics.DrawLine(pen, 5, 18, 8, 21);
                graphics.DrawLine(pen, 4, 22, 9, 21);
                break;
            case "document-edit":
                graphics.DrawRectangle(pen, 4, 3, 13, 17);
                graphics.DrawLine(pen, 6, 18, 17, 7);
                graphics.DrawLine(pen, 8, 20, 19, 9);
                break;
            case "trash":
                graphics.DrawLine(pen, 5, 6, 19, 6);
                graphics.DrawRectangle(pen, 7, 6, 10, 14);
                graphics.DrawLine(pen, 10, 9, 10, 17);
                graphics.DrawLine(pen, 14, 9, 14, 17);
                break;
            case "audit":
            case "audit-busy":
                graphics.DrawRectangle(pen, 5, 5, 14, 16);
                graphics.DrawRectangle(pen, 9, 3, 6, 4);
                graphics.DrawLine(pen, 8, 11, 10, 13);
                graphics.DrawLine(pen, 10, 13, 15, 8);
                if (type == "audit-busy")
                {
                    using var busyBrush = new SolidBrush(Color.FromArgb(201, 183, 106));
                    graphics.FillEllipse(busyBrush, 16, 2, 7, 7);
                }
                break;
            case "log":
                graphics.DrawRectangle(pen, 4, 4, 16, 16);
                graphics.DrawLine(pen, 8, 9, 16, 9);
                graphics.DrawLine(pen, 8, 13, 16, 13);
                graphics.DrawLine(pen, 8, 17, 14, 17);
                break;
            case "info":
                graphics.DrawEllipse(pen, 3, 3, 18, 18);
                graphics.FillEllipse(brush, 11, 7, 2, 2);
                graphics.DrawLine(pen, 12, 11, 12, 17);
                break;
            case "email":
                graphics.DrawRectangle(pen, 3, 6, 18, 13);
                graphics.DrawLine(pen, 4, 7, 12, 14);
                graphics.DrawLine(pen, 20, 7, 12, 14);
                break;
            case "calendar-check":
                graphics.DrawRectangle(pen, 4, 6, 16, 15);
                graphics.DrawLine(pen, 4, 10, 20, 10);
                graphics.DrawLine(pen, 8, 3, 8, 8);
                graphics.DrawLine(pen, 16, 3, 16, 8);
                graphics.DrawLine(pen, 8, 15, 11, 18);
                graphics.DrawLine(pen, 11, 18, 17, 12);
                break;
            case "refresh":
                graphics.DrawArc(pen, 4, 4, 16, 16, 35, 250);
                graphics.DrawLine(pen, 18, 3, 21, 8);
                graphics.DrawLine(pen, 21, 8, 16, 8);
                graphics.DrawArc(pen, 4, 4, 16, 16, 215, 250);
                graphics.DrawLine(pen, 6, 21, 3, 16);
                graphics.DrawLine(pen, 3, 16, 8, 16);
                break;
            case "download":
                graphics.DrawLine(pen, 12, 3, 12, 15);
                graphics.DrawLine(pen, 7, 11, 12, 16);
                graphics.DrawLine(pen, 17, 11, 12, 16);
                graphics.DrawLine(pen, 5, 20, 19, 20);
                graphics.DrawLine(pen, 5, 17, 5, 20);
                graphics.DrawLine(pen, 19, 17, 19, 20);
                break;
        }
        return image;
    }

    private static Bitmap CreateExternalAccessIcon(Color color)
    {
        var image = new Bitmap(16, 16);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, 2, 2, 12, 12);
        return image;
    }

    private static Color BlendWithWhite(Color color, float whiteAmount)
    {
        whiteAmount = Math.Clamp(whiteAmount, 0F, 1F);
        return Color.FromArgb(
            (int)(color.R * (1F - whiteAmount) + 255 * whiteAmount),
            (int)(color.G * (1F - whiteAmount) + 255 * whiteAmount),
            (int)(color.B * (1F - whiteAmount) + 255 * whiteAmount));
    }

    private sealed class AuditStatusDot : Control
    {
        private static readonly Color InstitutionalGold = Color.FromArgb(201, 183, 106);

        public AuditStatusDot()
        {
            Size = new Size(9, 9);
            Visible = false;
            TabStop = false;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint, true);
            using var circle = new System.Drawing.Drawing2D.GraphicsPath();
            circle.AddEllipse(0, 0, Width, Height);
            Region = new Region(circle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(InstitutionalGold);
            e.Graphics.FillEllipse(brush, 0, 0, Width, Height);
        }
    }

    private static PictureBox CreateHeaderIcon(string type)
    {
        var image = new Bitmap(50, 50);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        if (type == "user")
        {
            using var background = new SolidBrush(Color.FromArgb(220, 235, 253));
            using var foreground = new SolidBrush(Color.FromArgb(0, 120, 212));
            graphics.FillEllipse(background, 1, 1, 48, 48);
            graphics.FillEllipse(foreground, 19, 12, 12, 12);
            graphics.FillPie(foreground, 11, 25, 28, 22, 180, 180);
        }
        else
        {
            using var upperSphere = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(12, 1, 27, 27),
                Color.FromArgb(25, 213, 207),
                Color.FromArgb(0, 91, 112),
                90F);
            using var rightSphere = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(26, 14, 23, 27),
                Color.FromArgb(31, 220, 214),
                Color.FromArgb(0, 105, 124),
                90F);
            using var lowerSphere = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(18, 26, 23, 23),
                Color.FromArgb(42, 224, 218),
                Color.FromArgb(0, 137, 151),
                90F);
            graphics.FillEllipse(upperSphere, 12, 1, 27, 29);
            graphics.FillEllipse(rightSphere, 26, 14, 23, 27);
            graphics.FillEllipse(lowerSphere, 18, 26, 23, 23);

            RectangleF tileBounds = new(1, 21, 27, 27);
            using System.Drawing.Drawing2D.GraphicsPath tile = RoundedRectangle(tileBounds, 5F);
            using var tileBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                tileBounds,
                Color.FromArgb(11, 174, 174),
                Color.FromArgb(0, 91, 111),
                90F);
            graphics.FillPath(tileBrush, tile);
            using var letterFont = AppTypography.Create(18F, FontStyle.Bold, GraphicsUnit.Pixel);
            using var letterBrush = new SolidBrush(Color.White);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            graphics.DrawString("S", letterFont, letterBrush, tileBounds, format);
        }

        return new PictureBox
        {
            Image = image,
            Location = new Point(UiMetrics.CardPadding, 28),
            Size = new Size(50, 50),
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent
        };
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
    {
        float diameter = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static PictureBox CreateIdentityIcon(string type, Color color)
    {
        var image = new Bitmap(22, 22);
        using Graphics graphics = Graphics.FromImage(image);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var pen = new Pen(color, 1.8F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        using var brush = new SolidBrush(color);
        if (type == "email")
        {
            graphics.DrawRectangle(pen, 2, 5, 18, 13);
            graphics.DrawLine(pen, 3, 6, 11, 13);
            graphics.DrawLine(pen, 19, 6, 11, 13);
        }
        else
        {
            graphics.FillEllipse(brush, 8, 3, 7, 7);
            graphics.FillPie(brush, 4, 11, 15, 10, 180, 180);
        }

        return new PictureBox
        {
            Image = image,
            Size = new Size(22, 22),
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent
        };
    }

    private static Label CreateLabel(string text, float size, FontStyle style) => new()
    {
        Text = text,
        Font = AppTypography.Create(size, style),
        BackColor = Color.Transparent
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using (var titleBackground = new SolidBrush(_palette.Primary))
        {
            e.Graphics.FillRectangle(
                titleBackground,
                0,
                MaterialStatusBarHeight,
                ClientSize.Width,
                MaterialActionBarHeight);
        }
        using (var titleFont = AppTypography.Create(
                   AppTypography.ApplicationTitleSize,
                   FontStyle.Bold))
        {
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                titleFont,
                new Rectangle(
                    16,
                    MaterialStatusBarHeight - 3,
                    Math.Max(0, ClientSize.Width - 32),
                    MaterialActionBarHeight),
                Color.White,
                TextFormatFlags.NoPadding
                | TextFormatFlags.Left
                | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine);
        }
        using var accent = new SolidBrush(_palette.InstitutionalAccent);
        e.Graphics.FillRectangle(
            accent,
            0,
            0,
            ClientSize.Width,
            InstitutionalTopBorderHeight);
        InstitutionalWindowControls.DrawBackNavigation(this, e.Graphics, _backHovered);
        InstitutionalWindowControls.Draw(this, e.Graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (InstitutionalWindowControls.BackButtonBounds.Contains(e.Location))
        {
            _skipExitConfirmation = true;
            DialogResult = DialogResult.Cancel;
            Hide();
            return;
        }
        if (InstitutionalWindowControls.TryHandleClick(this, e.Location)) return;
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hovered = InstitutionalWindowControls.BackButtonBounds.Contains(e.Location);
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

    private static DashboardActionButton CreateButton(string text, bool accent) => new()
    {
        Text = text,
        IsDanger = accent
    };

    private static DashboardActionButton CreateSmallButton(string text) => new()
    {
        Text = text
    };

    private sealed class InstitutionalContextMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Color _selectionColor;

        public InstitutionalContextMenuRenderer(Color selectionColor)
        {
            _selectionColor = selectionColor;
            RoundedEdges = false;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Color background = e.Item.Selected && e.Item.Enabled
                ? _selectionColor
                : e.ToolStrip?.BackColor ?? Color.White;
            using var brush = new SolidBrush(background);
            e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = !e.Item.Enabled
                ? Color.FromArgb(145, 145, 145)
                : e.Item.Selected
                    ? Color.White
                    : Color.FromArgb(40, 40, 40);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(185, 187, 190));
            e.Graphics.DrawRectangle(
                pen,
                0,
                0,
                Math.Max(0, e.ToolStrip.Width - 1),
                Math.Max(0, e.ToolStrip.Height - 1));
        }
    }

    internal sealed class DashboardActionButton : Control
    {
        private bool _hovered;
        private bool _pressed;
        private Image? _icon;
        private int _extraHorizontalPadding;
        private int _fixedWidth;
        private readonly Font _displayFont;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool IsDanger { get; init; }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color PrimaryColor { get; set; } = Color.FromArgb(62, 88, 133);
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color PrimaryForeColor { get; set; } = Color.White;
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color DangerColor { get; set; } = Color.FromArgb(180, 75, 121);
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color DisabledBackColor { get; set; } = Color.FromArgb(228, 228, 228);
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color DisabledForeColor { get; set; } = Color.FromArgb(145, 145, 145);
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool AlignContentLeft { get; set; }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int ExtraHorizontalPadding
        {
            get => _extraHorizontalPadding;
            set
            {
                _extraHorizontalPadding = Math.Max(0, value);
                Size = GetPreferredSize(Size.Empty);
                Invalidate();
            }
        }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int FixedWidth
        {
            get => _fixedWidth;
            set
            {
                _fixedWidth = Math.Max(0, value);
                Size = GetPreferredSize(Size.Empty);
                Invalidate();
            }
        }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int IconVerticalOffset { get; set; }
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Image? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                Size = GetPreferredSize(Size.Empty);
                Invalidate();
            }
        }

        public DashboardActionButton()
        {
            AutoSize = false;
            _displayFont = AppTypography.Create(AppTypography.ButtonSize);
            Font = _displayFont;
            Height = DashboardButtonHeight;
            Cursor = Cursors.Hand;
            TabStop = true;
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
                true);
            Size = GetPreferredSize(Size.Empty);
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size textSize = TextRenderer.MeasureText(
                string.IsNullOrWhiteSpace(Text) ? " " : Text,
                _displayFont,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            int iconWidth = _icon is null ? 0 : 18 + 7;
            return new Size(
                // TextRenderer con Calibri può richiedere alcuni pixel aggiuntivi
                // rispetto alla misura nominale, soprattutto alle estremità.
                FixedWidth > 0
                    ? FixedWidth
                    : Math.Max(74, textSize.Width + iconWidth + 30 + ExtraHorizontalPadding),
                DashboardButtonHeight);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_displayFont is null) return;
            if (!ReferenceEquals(Font, _displayFont))
            {
                Font = _displayFont;
                return;
            }
            Size = GetPreferredSize(Size.Empty);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Size = GetPreferredSize(Size.Empty);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Space or Keys.Enter)
            {
                _pressed = true;
                e.Handled = true;
                Invalidate();
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (_pressed && e.KeyCode is Keys.Space or Keys.Enter)
            {
                _pressed = false;
                e.Handled = true;
                Invalidate();
                OnClick(EventArgs.Empty);
            }
            base.OnKeyUp(e);
        }

        public void ResetVisualState()
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(ResolveOpaqueBackground(Parent));
            Color baseColor = IsDanger ? DangerColor : PrimaryColor;
            Color background = Enabled
                ? _pressed
                    ? Blend(baseColor, Color.Black, 0.16F)
                    : _hovered
                        ? Blend(baseColor, Color.White, 0.12F)
                        : baseColor
                : DisabledBackColor;
            Color foreground = Enabled
                ? IsDanger ? Color.White : PrimaryForeColor
                : DisabledForeColor;

            Rectangle buttonBounds = new(
                0,
                0,
                Math.Max(0, ClientSize.Width - 1),
                Math.Max(0, ClientSize.Height - 1));
            using (var buttonPath = RoundedRectangle(buttonBounds, 7F))
            using (var fill = new SolidBrush(background))
            using (var border = new Pen(
                       Enabled
                           ? Blend(background, Color.Black, 0.14F)
                           : Blend(DisabledBackColor, Color.Black, 0.08F),
                       1F))
            {
                e.Graphics.FillPath(fill, buttonPath);
                e.Graphics.DrawPath(border, buttonPath);
            }

            int contentWidth = TextRenderer.MeasureText(
                Text,
                _displayFont,
                Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            if (_icon is not null) contentWidth += 25;
            int left = AlignContentLeft
                ? 10
                : Math.Max(7, (Width - contentWidth) / 2);
            if (_icon is not null)
            {
                using var attributes = new System.Drawing.Imaging.ImageAttributes();
                float red = foreground.R / 255F;
                float green = foreground.G / 255F;
                float blue = foreground.B / 255F;
                attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new[]
                {
                    new[] { red, 0F, 0F, 0F, 0F },
                    new[] { 0F, green, 0F, 0F, 0F },
                    new[] { 0F, 0F, blue, 0F, 0F },
                    new[] { 0F, 0F, 0F, 1F, 0F },
                    new[] { 0F, 0F, 0F, 0F, 1F }
                }));
                e.Graphics.DrawImage(
                    _icon,
                    new Rectangle(left, (Height - 18) / 2 + IconVerticalOffset, 18, 18),
                    0,
                    0,
                    _icon.Width,
                    _icon.Height,
                    GraphicsUnit.Pixel,
                    attributes);
                left += 25;
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                _displayFont,
                new Rectangle(left, 0, Math.Max(0, Width - left - 7), Height),
                foreground,
                TextFormatFlags.NoPadding | TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _displayFont.Dispose();
            base.Dispose(disposing);
        }

        private static Color ResolveOpaqueBackground(Control? control)
        {
            Control? current = control;
            while (current is not null)
            {
                Color color = current.BackColor;
                if (color.A == 255 && color != Color.Transparent) return color;
                current = current.Parent;
            }
            return SystemColors.Control;
        }
    }

    private sealed class WorkspaceSurface : WinFormsPanel
    {
        private readonly Color _surfaceColor;
        private readonly Color _borderColor;

        public WorkspaceSurface(Color surfaceColor, Color borderColor)
        {
            _surfaceColor = surfaceColor;
            _borderColor = borderColor;
            BackColor = Color.Transparent;
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle surfaceBounds = new(
                0,
                0,
                Math.Max(0, ClientSize.Width),
                Math.Max(0, ClientSize.Height));
            using var surface = new SolidBrush(_surfaceColor);
            e.Graphics.FillRectangle(surface, surfaceBounds);
            using var border = new Pen(_borderColor);
            e.Graphics.DrawRectangle(
                border,
                surfaceBounds.Left,
                surfaceBounds.Top,
                Math.Max(0, surfaceBounds.Width - 1),
                Math.Max(0, surfaceBounds.Height - 1));
        }
    }

    private sealed class WorkspaceSectionPanel : WinFormsPanel
    {
        private readonly Color _surfaceColor;
        private readonly Color _leftSeparator;

        public WorkspaceSectionPanel(Color surfaceColor, Color leftSeparator)
        {
            _surfaceColor = surfaceColor;
            _leftSeparator = leftSeparator;
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(_surfaceColor);
            if (!_leftSeparator.IsEmpty)
            {
                using var separator = new Pen(_leftSeparator);
                e.Graphics.DrawLine(separator, 0, 0, 0, ClientSize.Height);
            }
        }
    }

    private sealed class WorkspaceActionPanel : WinFormsPanel
    {
        private readonly Color _surfaceColor;
        private bool _restoringColor;

        public WorkspaceActionPanel(Color surfaceColor)
        {
            _surfaceColor = surfaceColor;
            BackColor = surfaceColor;
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            if (_restoringColor || BackColor == _surfaceColor) return;
            _restoringColor = true;
            BackColor = _surfaceColor;
            _restoringColor = false;
        }

        protected override void OnPaintBackground(PaintEventArgs e) =>
            e.Graphics.Clear(_surfaceColor);
    }

    private sealed class WorkspaceActionFlowPanel : FlowLayoutPanel
    {
        private readonly Color _surfaceColor;
        private bool _restoringColor;

        public WorkspaceActionFlowPanel(Color surfaceColor)
        {
            _surfaceColor = surfaceColor;
            BackColor = surfaceColor;
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            if (_restoringColor || BackColor == _surfaceColor) return;
            _restoringColor = true;
            BackColor = _surfaceColor;
            _restoringColor = false;
        }

        protected override void OnPaintBackground(PaintEventArgs e) =>
            e.Graphics.Clear(_surfaceColor);
    }
}
