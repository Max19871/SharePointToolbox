using Microsoft.Extensions.Options;
using SharePointToolbox.Configuration;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.UI.Theming;
using System.Net.Mail;

namespace SharePointToolbox.UI;

public sealed class PressReviewForm : InstitutionalMaterialForm
{
    private readonly PressReviewService _service;
    private readonly SharePointService _auditService;
    private readonly AuthenticationService _authenticationService;
    private readonly ApplicationAuthorizationSession _authorizationSession;
    private readonly PressReviewOptions _options;
    private readonly AppThemePalette _palette;
    private readonly AppThemeManager _themeManager;
    private readonly DateTimePicker _reviewDate = new();
    private readonly ListView _files = new();
    private readonly DropArea _dropArea;
    private readonly MainForm.DashboardActionButton _removeButton;
    private readonly MainForm.DashboardActionButton _downloadButton;
    private readonly MainForm.DashboardActionButton _clearButton;
    private readonly MainForm.DashboardActionButton _newButton;
    private readonly MainForm.DashboardActionButton _archiveButton;
    private readonly MainForm.DashboardActionButton _publishButton;
    private readonly Label _status = new();
    private bool _operationRunning;
    private bool _connected;
    private bool _returningToHub;
    private bool _changingDateInternally;
    private DateTime? _loadedReviewDate;
    private DateTime _lastAcceptedDate = DateTime.Today;
    private readonly HashSet<string> _pendingDeletionIds =
        new(StringComparer.OrdinalIgnoreCase);
    public bool ExitApplicationRequested { get; private set; }
    private sealed record FileSelection(
        string Name,
        long Size,
        string? LocalPath,
        string? RemoteId);

    public PressReviewForm(
        PressReviewService service,
        SharePointService auditService,
        AuthenticationService authenticationService,
        ApplicationAuthorizationSession authorizationSession,
        IOptions<PressReviewOptions> options,
        AppThemeManager theme)
    {
        _service = service;
        _auditService = auditService;
        _authenticationService = authenticationService;
        _authorizationSession = authorizationSession;
        _options = options.Value;
        _palette = theme.Palette;
        _themeManager = theme;
        Text = "Giovanardi Studio Legale Rassegna Stampa";
        ShowBackNavigation = true;
        ClientSize = new Size(820, 650);
        StartPosition = FormStartPosition.CenterScreen;
        Sizable = false;
        MaximizeBox = false;
        MinimizeBox = true;
        theme.ApplyTo(this);
        _dropArea = new DropArea(_palette);
        _removeButton = CreateReviewButton("Rimuovi selezionato", "trash", danger: true);
        _downloadButton = CreateReviewButton("Scarica selezionato", "download");
        _clearButton = CreateReviewButton("Svuota elenco", "trash");
        _newButton = CreateReviewButton("Nuova rassegna", "folder-plus");
        _archiveButton = CreateReviewButton("Archivio rassegne", "folder-open");
        _publishButton = CreateReviewButton("Pubblica e invia", "audit");
        BuildLayout();
        FormClosing += ConfirmClose;
        Shown += async (_, _) =>
        {
            if (_service.IsVerified)
            {
                _connected = true;
                _status.Text = $"Pronto · destinatario di prova: {_options.NotificationRecipient}";
                UpdateActions();
                return;
            }
            await VerifyConnectionAsync();
        };
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            Padding = new Padding(20, 12, 20, 16),
            BackColor = _palette.IsDark ? _palette.SurfaceDark : _palette.SurfaceLight
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 175));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var topBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(4, 8, 4, 5)
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var dateHost = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        dateHost.Controls.Add(new Label
        {
            Text = "Data della rassegna",
            AutoSize = true,
            Font = AppTypography.Create(11F, FontStyle.Bold),
            Margin = new Padding(0, 7, 16, 0)
        });
        _reviewDate.Format = DateTimePickerFormat.Custom;
        _reviewDate.CustomFormat = "dd/MM/yyyy";
        _reviewDate.Width = 128;
        _reviewDate.Font = AppTypography.Create(11F);
        _reviewDate.Value = DateTime.Today;
        _reviewDate.ValueChanged += (_, _) => ReviewDateChanged();
        dateHost.Controls.Add(_reviewDate);

        var navigation = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        _newButton.Width = 145;
        _newButton.Margin = new Padding(0, 0, 12, 0);
        _newButton.Click += (_, _) => StartNewReview();
        _archiveButton.Width = 165;
        _archiveButton.Margin = new Padding(0, 0, 12, 0);
        _archiveButton.Click += async (_, _) => await OpenArchiveAsync();
        navigation.Controls.Add(_newButton);
        navigation.Controls.Add(_archiveButton);
        topBar.Controls.Add(dateHost, 0, 0);
        topBar.Controls.Add(navigation, 2, 0);

        _dropArea.Dock = DockStyle.Fill;
        _dropArea.Margin = new Padding(4, 8, 4, 8);
        _dropArea.FilesDropped += (_, paths) => AddFiles(paths);
        _dropArea.BrowseRequested += (_, _) => BrowseFiles();

        _files.Dock = DockStyle.Fill;
        _files.View = View.Details;
        _files.FullRowSelect = true;
        _files.HideSelection = false;
        _files.BorderStyle = BorderStyle.FixedSingle;
        _files.BackColor = Color.White;
        _files.Columns.Add("Documento PDF", 500);
        _files.Columns.Add("Dimensione", 120);
        _files.Columns.Add("Stato", 130);
        InstitutionalSelectionStyle.Apply(_files);
        _files.SelectedIndexChanged += (_, _) => UpdateActions();

        var actionsBar = new Panel
        {
            Dock = DockStyle.Fill
        };
        var listActions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Location = new Point(4, 8),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0)
        };
        _removeButton.Width = 180;
        _downloadButton.Width = 180;
        _clearButton.Width = 140;
        _removeButton.Click += (_, _) => RemoveSelected();
        _downloadButton.Click += async (_, _) => await DownloadSelectedAsync();
        _clearButton.Click += (_, _) => ClearAll();
        listActions.Controls.Add(_removeButton);
        listActions.Controls.Add(_downloadButton);
        listActions.Controls.Add(_clearButton);
        _publishButton.FixedWidth = 170;
        _publishButton.Margin = new Padding(0);
        _publishButton.Click += async (_, _) => await PublishAsync();
        actionsBar.Controls.Add(listActions);
        actionsBar.Controls.Add(_publishButton);
        actionsBar.Resize += (_, _) =>
            _publishButton.Location = new Point(
                actionsBar.ClientSize.Width - _publishButton.Width - 4,
                8);

        _status.Dock = DockStyle.Fill;
        _status.Margin = new Padding(0, 8, 0, 0);
        _status.Font = AppTypography.Create(9.5F);
        _status.ForeColor = _palette.IsDark ? _palette.SecondaryTextDark : _palette.SecondaryTextLight;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Text = $"Destinatario di prova: {_options.NotificationRecipient}";

        root.Controls.Add(topBar, 0, 0);
        root.Controls.Add(_dropArea, 0, 1);
        root.Controls.Add(_files, 0, 2);
        root.Controls.Add(actionsBar, 0, 3);
        root.Controls.Add(_status, 0, 4);
        Controls.Add(root);
        UpdateActions();
    }

    private async Task VerifyConnectionAsync()
    {
        SetBusy(true, "Connessione al sito Rassegna Stampa…");
        try
        {
            await _service.VerifyAsync();
            _connected = true;
            _status.Text = $"Pronto · destinatario di prova: {_options.NotificationRecipient}";
        }
        catch (Exception ex)
        {
            _connected = false;
            AppMessageBox.Show(ex.Message, "Rassegna Stampa non disponibile",
                MessageBoxButtons.OK, MessageBoxIcon.Warning, this);
            _status.Text = "Connessione al sito non riuscita.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BrowseFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Seleziona i PDF della rassegna stampa",
            Filter = "Documenti PDF (*.pdf)|*.pdf",
            Multiselect = true,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            AddFiles(dialog.FileNames);
    }

    private async Task OpenArchiveAsync()
    {
        if (HasUnsavedChanges() &&
            AppMessageBox.Show(
                "Le modifiche correnti non sono state salvate. Aprire comunque l’archivio?",
                "Modifiche non salvate",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                this) != DialogResult.Yes)
            return;

        using var archive = new PressReviewArchiveForm(_service, _themeManager);
        if (archive.ShowDialog(this) == DialogResult.OK && archive.SelectedDate is DateTime date)
            await LoadReviewAsync(date);
    }

    private void StartNewReview()
    {
        if (HasUnsavedChanges() &&
            AppMessageBox.Show(
                "Sono presenti modifiche non salvate. Predisporre comunque una nuova rassegna e annullarle?",
                "Nuova rassegna",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                this) != DialogResult.Yes)
            return;

        _files.Items.Clear();
        _pendingDeletionIds.Clear();
        _loadedReviewDate = null;
        _changingDateInternally = true;
        _reviewDate.Value = DateTime.Today;
        _changingDateInternally = false;
        _lastAcceptedDate = DateTime.Today;
        _publishButton.Text = "Pubblica e invia";
        _status.Text =
            $"Nuova rassegna del {DateTime.Today:dd/MM/yyyy} · destinatario: {_options.NotificationRecipient}";
        UpdateActions();
        _dropArea.Focus();
    }

    private async Task LoadReviewAsync(DateTime date)
    {
        SetBusy(true, $"Caricamento della rassegna del {date:dd/MM/yyyy}…");
        try
        {
            IReadOnlyList<PressReviewService.ReviewFile> files =
                await _service.GetReviewFilesAsync(date);
            _files.BeginUpdate();
            _files.Items.Clear();
            _pendingDeletionIds.Clear();
            foreach (PressReviewService.ReviewFile file in files)
            {
                var selection = new FileSelection(file.Name, file.Size, null, file.Id);
                var item = new ListViewItem(file.Name) { Tag = selection };
                item.SubItems.Add(FormatSize(file.Size));
                item.SubItems.Add("Pubblicato");
                _files.Items.Add(item);
            }
            _files.EndUpdate();
            _changingDateInternally = true;
            _reviewDate.Value = date;
            _changingDateInternally = false;
            _lastAcceptedDate = date;
            _loadedReviewDate = date;
            _publishButton.Text = "Reinvia link";
            _status.Text = $"Rassegna del {date:dd/MM/yyyy} · documenti pubblicati: {files.Count}";
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(ex.Message, "Rassegna non disponibile",
                MessageBoxButtons.OK, MessageBoxIcon.Error, this);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ReviewDateChanged()
    {
        if (_changingDateInternally) return;
        DateTime newDate = _reviewDate.Value.Date;
        if (_loadedReviewDate.HasValue)
        {
            _changingDateInternally = true;
            _reviewDate.Value = _lastAcceptedDate;
            _changingDateInternally = false;
            return;
        }

        _lastAcceptedDate = newDate;
        _publishButton.Text = "Pubblica e invia";
        _status.Text =
            $"Nuova rassegna del {newDate:dd/MM/yyyy} · documenti predisposti: {_files.Items.Count} · destinatario: {_options.NotificationRecipient}";
        UpdateActions();
    }

    private bool HasUnsavedChanges() =>
        _pendingDeletionIds.Count > 0 ||
        _files.Items.Cast<ListViewItem>().Any(item =>
            item.Tag is FileSelection { LocalPath: not null });

    private void AddFiles(IEnumerable<string> paths)
    {
        string[] rejected = paths
            .Where(path => !string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFileName(path) ?? path)
            .ToArray();
        HashSet<string> existing = _files.Items.Cast<ListViewItem>()
            .Select(item => (item.Tag as FileSelection)?.LocalPath ?? string.Empty)
            .Where(path => path.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string path in paths.Where(File.Exists))
        {
            if (!string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase)
                || !existing.Add(Path.GetFullPath(path)))
                continue;
            var info = new FileInfo(path);
            var selection = new FileSelection(info.Name, info.Length, info.FullName, null);
            var item = new ListViewItem(info.Name) { Tag = selection };
            item.SubItems.Add(FormatSize(info.Length));
            item.SubItems.Add("Da caricare");
            _files.Items.Add(item);
        }
        UpdateActions();
        if (rejected.Length > 0)
            AppMessageBox.Show(
                "Sono stati esclusi i file non PDF:\r\n" + string.Join("\r\n", rejected),
                "File non validi", MessageBoxButtons.OK, MessageBoxIcon.Warning, this);
    }

    private async Task PublishAsync()
    {
        string[] paths = _files.Items.Cast<ListViewItem>()
            .Select(item => (item.Tag as FileSelection)?.LocalPath)
            .OfType<string>()
            .ToArray();
        if (_files.Items.Count == 0)
        {
            AppMessageBox.Show(
                "La rassegna deve contenere almeno un documento PDF.",
                "Rassegna vuota",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning,
                this);
            return;
        }

        DateTime date = _reviewDate.Value.Date;
        bool isExistingReview = _loadedReviewDate.HasValue;
        bool resendOnly = isExistingReview && !HasUnsavedChanges();
        if (resendOnly)
        {
            await ResendReviewLinkAsync(date);
            return;
        }
        if (paths.Length == 0 && _pendingDeletionIds.Count == 0) return;
        IReadOnlySet<string> existing = await _service.GetExistingFileNamesAsync(date);
        string[] duplicates = paths
            .Select(path => Path.GetFileName(path) ?? path)
            .Where(existing.Contains)
            .ToArray();
        if (duplicates.Length > 0)
        {
            DialogResult replace = AppMessageBox.Show(
                "I seguenti documenti esistono già e verranno sostituiti:\r\n\r\n" +
                string.Join("\r\n", duplicates) +
                (isExistingReview
                    ? "\r\n\r\nConfermi il salvataggio delle modifiche?"
                    : "\r\n\r\nConfermi la pubblicazione?"),
                "Documenti già presenti",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                this);
            if (replace != DialogResult.Yes) return;
        }
        if (AppMessageBox.Show(
                isExistingReview
                    ? $"Salvare le modifiche alla rassegna del {date:dd/MM/yyyy}?\r\n\r\nNon verrà inviata una nuova email."
                    : $"Confermi la pubblicazione di {paths.Length} PDF per la rassegna del {date:dd/MM/yyyy} e l’invio della notifica a:\r\n\r\n{_options.NotificationRecipient}?",
                isExistingReview ? "Conferma salvataggio" : "Conferma pubblicazione e invio",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                this) != DialogResult.Yes)
            return;

        SetBusy(true, "Preparazione della pubblicazione…");
        var progress = new Progress<(int Percentage, string Message)>(value =>
        {
            _status.Text = $"{value.Message}  {value.Percentage}%";
        });
        try
        {
            PressReviewService.PublicationResult result =
                await _service.PublishAsync(
                    date,
                    paths,
                    _pendingDeletionIds.ToArray(),
                    sendNotification: !isExistingReview,
                    progress: progress);
            if (_authorizationSession.User is not null)
            {
                await _auditService.RecordActivityAsync(
                    _authorizationSession.User,
                    isExistingReview
                        ? "Modifica rassegna stampa"
                        : "Pubblicazione rassegna stampa",
                    "Successo",
                    null,
                    $"Rassegna stampa {date:dd/MM/yyyy}",
                    isExistingReview ? null : _options.NotificationRecipient,
                    $"Caricati o sostituiti {result.UploadedFiles.Count} PDF: {string.Join(", ", result.UploadedFiles)}. Rimossi {_pendingDeletionIds.Count} PDF. " +
                    (isExistingReview ? "Nessuna nuova notifica inviata." : "Notifica inviata."));
            }
            AppMessageBox.Show(
                isExistingReview
                    ? $"Rassegna del {date:dd/MM/yyyy} aggiornata correttamente.\r\n\r\nDocumenti caricati o sostituiti: {result.UploadedFiles.Count}\r\nNessuna nuova email inviata."
                    : $"Rassegna del {date:dd/MM/yyyy} pubblicata correttamente.\r\n\r\nDocumenti caricati: {result.UploadedFiles.Count}\r\nNotifica inviata a: {_options.NotificationRecipient}",
                isExistingReview ? "Modifiche salvate" : "Pubblicazione completata",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                this);
            _status.Text = $"Ultima pubblicazione completata: {DateTime.Now:dd/MM/yyyy HH:mm}";
            await LoadReviewAsync(date);
        }
        catch (Exception ex)
        {
            if (_authorizationSession.User is not null)
            {
                try
                {
                    await _auditService.RecordActivityAsync(
                        _authorizationSession.User,
                        isExistingReview
                            ? "Modifica rassegna stampa"
                            : "Pubblicazione rassegna stampa",
                        "Errore",
                        null,
                        $"Rassegna stampa {date:dd/MM/yyyy}",
                        isExistingReview ? null : _options.NotificationRecipient,
                        ex.Message);
                }
                catch
                {
                    // L’errore principale di pubblicazione resta quello mostrato all’utente.
                }
            }
            AppMessageBox.Show(ex.Message, "Pubblicazione non riuscita",
                MessageBoxButtons.OK, MessageBoxIcon.Error, this);
            _status.Text = "Pubblicazione non completata.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RemoveSelected()
    {
        ListViewItem[] selected = _files.SelectedItems.Cast<ListViewItem>().ToArray();
        if (selected.Length == 0) return;
        if (_loadedReviewDate.HasValue && selected.Length == _files.Items.Count)
        {
            AppMessageBox.Show(
                "Una rassegna pubblicata deve conservare almeno un documento. Aggiungere prima il PDF sostitutivo oppure lasciare almeno un documento nell’elenco.",
                "Rassegna vuota non consentita",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning,
                this);
            return;
        }

        FileSelection[] published = selected
            .Select(item => item.Tag as FileSelection)
            .Where(selection => selection?.RemoteId is not null)
            .Cast<FileSelection>()
            .ToArray();
        if (published.Length > 0 &&
            AppMessageBox.Show(
                $"Confermi la rimozione di {published.Length} " +
                $"{(published.Length == 1 ? "documento già pubblicato" : "documenti già pubblicati")}?\r\n\r\n" +
                "La cancellazione da SharePoint verrà applicata soltanto premendo SALVA.",
                "Conferma rimozione",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                this) != DialogResult.Yes)
            return;

        foreach (ListViewItem item in selected)
        {
            if (item.Tag is FileSelection { RemoteId: not null } selection)
                _pendingDeletionIds.Add(selection.RemoteId);
            _files.Items.Remove(item);
        }
        if (published.Length > 0)
            _status.Text = $"Modifiche da salvare · documenti da rimuovere: {_pendingDeletionIds.Count}";
        UpdateActions();
    }

    private async Task DownloadSelectedAsync()
    {
        if (_loadedReviewDate is not DateTime reviewDate
            || _files.SelectedItems.Count != 1
            || _files.SelectedItems[0].Tag is not FileSelection
            {
                RemoteId: not null
            } selection)
            return;

        using var dialog = new SaveFileDialog
        {
            Title = "Salva il documento della rassegna stampa",
            Filter = "Documenti PDF (*.pdf)|*.pdf",
            FileName = selection.Name,
            AddExtension = true,
            DefaultExt = "pdf",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        SetBusy(true, $"Download di {selection.Name}â€¦");
        try
        {
            await _service.DownloadFileAsync(selection.RemoteId, dialog.FileName);
            if (_authorizationSession.User is not null)
            {
                await _auditService.RecordActivityAsync(
                    _authorizationSession.User,
                    "Download documento rassegna stampa",
                    "Successo",
                    null,
                    $"Rassegna stampa {reviewDate:dd/MM/yyyy}",
                    null,
                    $"Scaricato il documento '{selection.Name}'.");
            }
            _status.Text = $"Documento scaricato: {selection.Name}";
            AppMessageBox.Show(
                "Il documento selezionato è stato scaricato correttamente.",
                "Download completato",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                this);
        }
        catch (Exception ex)
        {
            if (_authorizationSession.User is not null)
            {
                try
                {
                    await _auditService.RecordActivityAsync(
                        _authorizationSession.User,
                        "Download documento rassegna stampa",
                        "Errore",
                        null,
                        $"Rassegna stampa {reviewDate:dd/MM/yyyy}",
                        null,
                        $"{selection.Name}: {ex.Message}");
                }
                catch
                {
                    // L'errore di download resta quello mostrato all'utente.
                }
            }
            AppMessageBox.Show(
                ex.Message,
                "Download non riuscito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                this);
            _status.Text = "Download del documento non completato.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ClearAll()
    {
        foreach (ListViewItem item in _files.Items)
            if (item.Tag is FileSelection { RemoteId: not null } selection)
                _pendingDeletionIds.Add(selection.RemoteId);
        _files.Items.Clear();
        UpdateActions();
    }

    private void UpdateActions()
    {
        bool hasChanges = HasUnsavedChanges();
        if (_loadedReviewDate.HasValue)
            _publishButton.Text = hasChanges ? "Salva" : "Reinvia link";
        else
            _publishButton.Text = "Pubblica e invia";
        _removeButton.Enabled = _files.SelectedItems.Count > 0 && !_operationRunning;
        _downloadButton.Enabled =
            _loadedReviewDate.HasValue
            && _files.SelectedItems.Count == 1
            && _files.SelectedItems[0].Tag is FileSelection { RemoteId: not null }
            && !_operationRunning;
        _clearButton.Enabled =
            _loadedReviewDate is null && _files.Items.Count > 0 && !_operationRunning;
        _publishButton.Enabled =
            _connected &&
            _files.Items.Count > 0 &&
            (hasChanges || _loadedReviewDate.HasValue) &&
            !_operationRunning;
        _newButton.Enabled =
            _connected &&
            !_operationRunning &&
            (_loadedReviewDate.HasValue || _files.Items.Count > 0);
        _archiveButton.Enabled = _connected && !_operationRunning;
        _reviewDate.Enabled = !_operationRunning && !_loadedReviewDate.HasValue;
    }

    private async Task ResendReviewLinkAsync(DateTime date)
    {
        using var recipientDialog = new PressReviewRecipientForm(
            date,
            _options.NotificationRecipient,
            _authenticationService,
            InternalUserDomain());
        if (recipientDialog.ShowDialog(this) != DialogResult.OK)
            return;
        string recipient = recipientDialog.Recipient;

        SetBusy(true, "Preparazione del reinvio…");
        var progress = new Progress<(int Percentage, string Message)>(value =>
            _status.Text = $"{value.Message}  {value.Percentage}%");
        try
        {
            await _service.ResendNotificationAsync(date, recipient, progress);
            if (_authorizationSession.User is not null)
            {
                await _auditService.RecordActivityAsync(
                    _authorizationSession.User,
                    "Reinvio link rassegna stampa",
                    "Successo",
                    null,
                    $"Rassegna stampa {date:dd/MM/yyyy}",
                    recipient,
                    "Collegamento alla rassegna reinviato senza modificare i documenti.");
            }
            AppMessageBox.Show(
                $"Collegamento alla rassegna del {date:dd/MM/yyyy} reinviato correttamente a {recipient}.",
                "Collegamento reinviato",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                this);
            _status.Text = $"Ultimo reinvio completato: {DateTime.Now:dd/MM/yyyy HH:mm}";
        }
        catch (Exception ex)
        {
            if (_authorizationSession.User is not null)
            {
                try
                {
                    await _auditService.RecordActivityAsync(
                        _authorizationSession.User,
                        "Reinvio link rassegna stampa",
                        "Errore",
                        null,
                        $"Rassegna stampa {date:dd/MM/yyyy}",
                        recipient,
                        ex.Message);
                }
                catch
                {
                    // L’errore di invio resta quello mostrato all’utente.
                }
            }
            AppMessageBox.Show(
                ex.Message,
                "Reinvio non riuscito",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                this);
            _status.Text = "Reinvio del collegamento non completato.";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _operationRunning = busy;
        _reviewDate.Enabled = !busy && !_loadedReviewDate.HasValue;
        _dropArea.Enabled = !busy;
        _newButton.Enabled = !busy && _connected;
        _archiveButton.Enabled = !busy && _connected;
        if (!string.IsNullOrWhiteSpace(status)) _status.Text = status;
        UseWaitCursor = busy;
        UpdateActions();
    }

    private string InternalUserDomain()
    {
        if (_authorizationSession.User is not null
            && MailAddress.TryCreate(
                _authorizationSession.User.Email,
                out MailAddress? current))
            return current.Host;

        if (MailAddress.TryCreate(
                _options.NotificationRecipient,
                out MailAddress? configured))
            return configured.Host;

        return string.Empty;
    }

    private void ConfirmClose(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason != CloseReason.UserClosing) return;
        if (_operationRunning)
        {
            e.Cancel = true;
            _returningToHub = false;
            return;
        }

        if (HasUnsavedChanges())
        {
            string message = _returningToHub
                ? "Sono presenti modifiche non salvate. Tornare comunque a Document Hub e annullarle?"
                : "Sono presenti modifiche non salvate. Chiudere comunque l’applicazione e annullarle?";
            if (AppMessageBox.Show(
                    message,
                    "Modifiche non salvate",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    this) != DialogResult.Yes)
            {
                e.Cancel = true;
                _returningToHub = false;
                return;
            }
        }
        else if (!_returningToHub &&
                 AppMessageBox.Show(
                     "Confermi di voler chiudere l’applicazione?",
                     "Conferma uscita",
                     MessageBoxButtons.YesNo,
                     MessageBoxIcon.Question,
                     this) != DialogResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        if (!_returningToHub)
            ExitApplicationRequested = true;
    }

    protected override void OnBackNavigationRequested()
    {
        _returningToHub = true;
        Close();
    }

    private MainForm.DashboardActionButton CreateReviewButton(
        string text,
        string icon,
        bool danger = false)
    {
        return ConfigureReviewButton(new MainForm.DashboardActionButton
        {
            Text = text,
            Icon = MainForm.CreateButtonIcon(icon),
            IsDanger = danger,
            ExtraHorizontalPadding = 6
        }, _palette);
    }

    private static MainForm.DashboardActionButton ConfigureReviewButton(
        MainForm.DashboardActionButton button,
        AppThemePalette palette)
    {
        button.PrimaryColor = palette.DashboardButtonBackground;
        button.PrimaryForeColor = palette.DashboardButtonForeground;
        button.DangerColor = palette.Danger;
        button.DisabledBackColor = palette.IsDark
            ? Color.FromArgb(70, 70, 70)
            : Color.FromArgb(228, 228, 228);
        button.DisabledForeColor = Color.FromArgb(145, 145, 145);
        return button;
    }

    private static string FormatSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return $"{value:0.##} {units[unit]}";
    }

    private sealed class DropArea : Panel
    {
        private readonly AppThemePalette _palette;
        private readonly MainForm.DashboardActionButton _browse;
        private bool _dragActive;
        public event EventHandler<string[]>? FilesDropped;
        public event EventHandler? BrowseRequested;

        public DropArea(AppThemePalette palette)
        {
            _palette = palette;
            AllowDrop = true;
            BackColor = Color.FromArgb(249, 249, 249);
            DoubleBuffered = true;
            _browse = ConfigureReviewButton(new MainForm.DashboardActionButton
            {
                Text = "Sfoglia…",
                Icon = MainForm.CreateButtonIcon("folder-open"),
                ExtraHorizontalPadding = 6
            }, palette);
            _browse.Width = 130;
            _browse.Click += (_, _) => BrowseRequested?.Invoke(this, EventArgs.Empty);
            Controls.Add(_browse);
            DragEnter += OnDragEnter;
            DragOver += OnDragEnter;
            DragLeave += (_, _) => { _dragActive = false; Invalidate(); };
            DragDrop += OnDragDrop;
            Resize += (_, _) =>
                _browse.Location = new Point((ClientSize.Width - _browse.Width) / 2, ClientSize.Height - 54);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Color border = _dragActive ? _palette.InstitutionalAccent : Color.FromArgb(130, 135, 142);
            float penWidth = _dragActive ? 7F : 5.5F;
            RectangleF bounds = ClientRectangle;
            float inset = penWidth / 2F + 2F;
            bounds.Inflate(-inset, -inset);
            const float radius = 15F;
            float perimeter =
                2F * (bounds.Width + bounds.Height - 4F * radius) +
                2F * MathF.PI * radius;
            float desiredPeriod = penWidth * (2.8F + 1.8F);
            int periods = Math.Max(1, (int)MathF.Round(perimeter / desiredPeriod));
            float periodUnits = perimeter / periods / penWidth;
            using var pen = new Pen(border, penWidth)
            {
                DashStyle = System.Drawing.Drawing2D.DashStyle.Custom,
                // Il periodo viene adattato al perimetro: il primo e l'ultimo
                // tratto coincidono senza creare un doppio segmento.
                DashPattern = new[] { periodUnits * 0.61F, periodUnits * 0.39F },
                DashCap = System.Drawing.Drawing2D.DashCap.Round,
                StartCap = System.Drawing.Drawing2D.LineCap.Round,
                EndCap = System.Drawing.Drawing2D.LineCap.Round,
                LineJoin = System.Drawing.Drawing2D.LineJoin.Round
            };
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using System.Drawing.Drawing2D.GraphicsPath borderPath =
                RoundedRectangle(bounds, radius);
            e.Graphics.DrawPath(pen, borderPath);
            string heading = _dragActive
                ? "Rilascia i PDF per aggiungerli"
                : "Trascina qui uno o più PDF";
            using Font headingFont = AppTypography.Create(14F, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics,
                heading,
                headingFont,
                new Rectangle(20, 25, Width - 40, 30),
                _palette.Primary,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using Font detailFont = AppTypography.Create(10F);
            TextRenderer.DrawText(
                e.Graphics,
                "Oppure utilizza il tasto Sfoglia per selezionare i documenti da caricare",
                detailFont,
                new Rectangle(20, 61, Width - 40, 26),
                Color.FromArgb(85, 85, 85),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundedRectangle(
            RectangleF bounds,
            float radius)
        {
            float diameter = Math.Min(
                radius * 2F,
                Math.Min(bounds.Width, bounds.Height));
            radius = diameter / 2F;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            float centerX = bounds.Left + bounds.Width / 2F;
            path.AddLine(centerX, bounds.Top, bounds.Right - radius, bounds.Top);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddLine(bounds.Right, bounds.Top + radius, bounds.Right, bounds.Bottom - radius);
            path.AddArc(
                bounds.Right - diameter,
                bounds.Bottom - diameter,
                diameter,
                diameter,
                0,
                90);
            path.AddLine(bounds.Right - radius, bounds.Bottom, bounds.Left + radius, bounds.Bottom);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.AddLine(bounds.Left, bounds.Bottom - radius, bounds.Left, bounds.Top + radius);
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddLine(bounds.Left + radius, bounds.Top, centerX, bounds.Top);
            path.CloseFigure();
            return path;
        }

        private void OnDragEnter(object? sender, DragEventArgs e)
        {
            bool valid = e.Data?.GetDataPresent(DataFormats.FileDrop) == true;
            e.Effect = valid ? DragDropEffects.Copy : DragDropEffects.None;
            _dragActive = valid;
            Invalidate();
        }

        private void OnDragDrop(object? sender, DragEventArgs e)
        {
            _dragActive = false;
            Invalidate();
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files)
                FilesDropped?.Invoke(this, files);
        }
    }
}
