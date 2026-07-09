using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.Models;
using SharePointToolbox.Helpers;
using SharePointToolbox.Configuration;

namespace SharePointToolbox.UI;

public partial class MainMaterialForm : MaterialForm
{
    // ==========================================
    // SERVIZI E DIPENDENZE (BACKEND ESISTENTE)
    // ==========================================
    private readonly AuthenticationService _authenticationService;
    private readonly SharePointService _sharePointService;
    private readonly MaterialSkinManager _materialSkinManager;

    // ==========================================
    // CONTROLLI SCHEDA GESTIONE PRATICHE (TAB 2)
    // ==========================================
    private MaterialTextBox2 txtCercaPratica;
    private MaterialListView listViewStoricoPratiche;
    private MaterialButton btnAggiornaPratiche;
    private MaterialButton btnCondividiPratica;
    private System.Collections.Generic.List<ListViewItem> _tutteLePratiche = new();

    // ==========================================
    // CONTROLLI SCHEDA GESTIONE CONDIVISIONI (TAB 3)
    // ==========================================
    private MaterialListView listViewInviti;
    private System.Collections.Generic.List<ListViewItem> _tuttiGliInviti = new();

    public MainMaterialForm(
        AuthenticationService authenticationService,
        SharePointService sharePointService)
    {
        InitializeComponent();

        //ConfiguraLayoutDashboard();

        _authenticationService = authenticationService;
        _sharePointService = sharePointService;

        _materialSkinManager = MaterialSkinManager.Instance;
        _materialSkinManager.AddFormToManage(this);
        _materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;

        _materialSkinManager.ColorScheme = new ColorScheme(
            Primary.Indigo700,
            Primary.Indigo900,
            Primary.Indigo500,
            Accent.Pink200,
            TextShade.WHITE
        );

        this.DrawerShowIconsWhenHidden = true;
        this.DrawerUseColors = false;
        this.DrawerWidth = 240;

        this.Text = AppConstants.AppName;

        UiLogger.Initialize(AddLogLine);

        SetupTabIcons();

        InizializzaTabPratiche();
        InizializzaTabCondivisioni();
    }

    private void MainMaterialForm_Load(object sender, EventArgs e)
    {
        ResetInterfacciaGrafica();
        UiLogger.Info($"Applicazione '{AppConstants.AppName}' avviata. Pronto per il login.");
    }

    private void ResetInterfacciaGrafica()
    {
        lblUtenteNome.Text = "Non autenticato";
        lblUtenteEmail.Text = "Effettua il login per iniziare";
        lblSitoNome.Text = "-";
        lblSitoUrl.Text = "Nessun sito SharePoint caricato";

        lblStatusConnesso.Text = "Disconnesso";
        lblStatusConnesso.ForeColor = Color.Red;
        lblStatusUtente.Text = "Utente: -";
        lblStatusTenant.Text = "Tenant: -";
        lblStatusSito.Text = "Sito: -";

        lblNoCartella.Visible = true;
        lblDettaglioPercorso.Text = "-";
        lblDettaglioID.Text = "-";
        lblDettaglioData.Text = "-";
        lblDettaglioCreatore.Text = "-";

        lblNoInterni.Visible = true;
        lblNoEsterni.Visible = true;
        listViewInterni.Items.Clear();
        listViewEsterni.Items.Clear();

        if (listViewInviti != null) listViewInviti.Items.Clear();
        _tuttiGliInviti.Clear();

        btnCreaCartella.Enabled = false;
        txtNomePratica.Enabled = false;
        if (btnCondividiPratica != null) btnCondividiPratica.Enabled = false;

        listViewLog.Columns.Clear();
        listViewLog.Columns.Add("", 30, HorizontalAlignment.Center);
        listViewLog.Columns.Add("Orario", 140, HorizontalAlignment.Left);
        listViewLog.Columns.Add("Messaggio di Registro", 800, HorizontalAlignment.Left);

        listViewInterni.Columns.Clear();
        listViewInterni.Columns.Add("Nome / Email", 220, HorizontalAlignment.Left);
        listViewInterni.Columns.Add("Ruolo", 130, HorizontalAlignment.Left);
        listViewInterni.Columns.Add("Tipo", 130, HorizontalAlignment.Left);

        listViewEsterni.Columns.Clear();
        listViewEsterni.Columns.Add("Nome / Email", 220, HorizontalAlignment.Left);
        listViewEsterni.Columns.Add("Ruolo", 130, HorizontalAlignment.Left);
        listViewEsterni.Columns.Add("Tipo", 130, HorizontalAlignment.Left);
    }

    private async void btnRiconnetti_Click(object sender, EventArgs e)
    {
        UiLogger.Info("Connessione a Microsoft 365 in corso...");
        try
        {
            var result = await _authenticationService.SignInAsync();

            lblUtenteNome.Text = result.Account.Username.Split('@')[0].Replace(".", " ");
            lblUtenteEmail.Text = result.Account.Username;

            UiLogger.Info($"Login effettuato con successo: {result.Account.Username}");

            DocumentLibrary library = await _sharePointService.GetDefaultDocumentLibraryAsync();

            lblSitoNome.Text = library.Name;
            lblSitoUrl.Text = "Sito SharePoint connesso correttamente.";
            UiLogger.Info($"Sito SharePoint agganciato: {library.Name}");

            lblStatusConnesso.Text = "✔ Connesso";
            lblStatusConnesso.ForeColor = Color.Green;
            lblStatusUtente.Text = $"Utente: {result.Account.Username}";
            lblStatusTenant.Text = $"Tenant: {result.TenantId ?? "Aziendale"}";
            lblStatusSito.Text = $"Sito: {library.Name}";

            CaricaUtentiEPermessiMockup();
            CaricaStoricoPraticheMockup();
            CaricaInvitiMockup();

            btnCreaCartella.Enabled = true;
            txtNomePratica.Enabled = true;
        }
        catch (Exception ex)
        {
            UiLogger.Info($"[ERRORE] Autenticazione fallita: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore di connessione", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnCreaCartella_Click(object sender, EventArgs e)
    {
        string inputName = txtNomePratica.Text;

        if (string.IsNullOrWhiteSpace(inputName))
        {
            UiLogger.Info("[Errore] Impossibile creare una cartella senza un nome.");
            MaterialMessageBox.Show("Inserisci un nome valido per la pratica.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnCreaCartella.Enabled = false;

            string finalName = await _sharePointService.CreateFolderAsync(inputName);

            string mockId = Guid.NewGuid().ToString("N").Substring(0, 16).ToUpper();
            string dataAttuale = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            string creatore = lblUtenteNome.Text != "Non autenticato" ? lblUtenteNome.Text : "Utente Sistema";

            MostraDettagliCartella(finalName, mockId, dataAttuale, creatore);
            AggiungiPraticaAStorico(finalName, dataAttuale);

            txtNomePratica.Text = string.Empty;
            UiLogger.Info($"[SUCCESSO] Cartella caricata nei dettagli della UI.");
        }
        catch (Exception ex)
        {
            UiLogger.Info($"[ERRORE] Fallimento durante la creazione: {ex.Message}");
            MaterialMessageBox.Show($"Errore Visualizzato: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCreaCartella.Enabled = true;
        }
    }

    private void AddLogLine(string text)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action<string>(AddLogLine), text);
            return;
        }

        string timestamp = DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss");
        string icona = "ℹ";

        if (text.Contains("[SUCCESSO]")) icona = "✔";
        if (text.Contains("[ERRORE]")) icona = "❌";

        var item = new ListViewItem(icona);
        item.SubItems.Add(timestamp);
        item.SubItems.Add(text.Replace("[SUCCESSO]", "").Replace("[ERRORE]", ""));

        listViewLog.Items.Add(item);
        listViewLog.EnsureVisible(listViewLog.Items.Count - 1);

        lblStatusAggiornamento.Text = $"Ultimo aggiornamento: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
    }

    private void btnPulisciLog_Click(object sender, EventArgs e)
    {
        listViewLog.Items.Clear();
    }

    private void MostraDettagliCartella(string nomeCartella, string idCartella, string data, string creatore)
    {
        lblNoCartella.Visible = false;
        lblDettaglioPercorso.Text = $"/Shared Documents/{nomeCartella}";
        lblDettaglioID.Text = idCartella;
        lblDettaglioData.Text = data;
        lblDettaglioCreatore.Text = creatore;
    }

    private void CaricaUtentiEPermessiMockup()
    {
        lblNoInterni.Visible = false;
        lblNoEsterni.Visible = false;
        listViewInterni.Items.Clear();
        listViewEsterni.Items.Clear();

        var utente1 = new ListViewItem("Avv. Marco Giovanardi");
        utente1.SubItems.Add("Proprietario");
        utente1.SubItems.Add("Membro Interno");

        var utente2 = new ListViewItem("Dott.ssa Laura Bianchi");
        utente2.SubItems.Add("Editor (Scrittura)");
        utente2.SubItems.Add("Membro Interno");

        var utente3 = new ListViewItem("Segreteria Generale");
        utente3.SubItems.Add("Lettore (Solo Vista)");
        utente3.SubItems.Add("Gruppo Aziendale");

        listViewInterni.Items.AddRange(new ListViewItem[] { utente1, utente2, utente3 });

        var esterno1 = new ListViewItem("mario.rossi@clientecorp.it");
        esterno1.SubItems.Add("Editor (Scrittura)");
        esterno1.SubItems.Add("Ospite Esterno");

        var esterno2 = new ListViewItem("consulente.fiscale@studioassociato.com");
        esterno2.SubItems.Add("Lettore (Solo Vista)");
        esterno2.SubItems.Add("Ospite Esterno");

        listViewEsterni.Items.AddRange(new ListViewItem[] { esterno1, esterno2 });
    }

    // ==========================================
    // LOGICA SCHEDA GESTIONE PRATICHE (TAB 2)
    // ==========================================
    private void InizializzaTabPratiche()
    {
        MaterialCard mainCard = new MaterialCard
        {
            Location = new Point(14, 14),
            Size = new Size(tabPratiche.Width - 28, tabPratiche.Height - 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(14)
        };
        tabPratiche.Controls.Add(mainCard);

        Label lblTitolo = new Label
        {
            Text = "Archivio Storico delle Pratiche Attive (Data Room)",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(63, 87, 133),
            Location = new Point(14, 14),
            AutoSize = true
        };
        mainCard.Controls.Add(lblTitolo);

        // 3. Campo di Ricerca con Filtro Real-Time (Accorciato per dare respiro ai tasti)
        txtCercaPratica = new MaterialTextBox2
        {
            Hint = "Filtra o cerca pratica per nome...",
            Location = new Point(14, 50),
            Size = new Size(mainCard.Width - 410, 48), // Ridotta la larghezza da -340 a -410
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        txtCercaPratica.TextChanged += TxtCercaPratica_TextChanged;
        mainCard.Controls.Add(txtCercaPratica);

        // 4. Pulsante Aggiorna (Allineato a Y=56 e distanziato)
        btnAggiornaPratiche = new MaterialButton
        {
            Text = "Aggiorna",
            Location = new Point(mainCard.Width - 365, 56), // Spostato a sinistra e centrato a Y=56
            Size = new Size(110, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Type = MaterialButton.MaterialButtonType.Outlined, // Cambiato a Outlined per dargli il bordo e non farlo sembrare "volante"
            HighEmphasis = false
        };
        btnAggiornaPratiche.Click += (s, e) => CaricaStoricoPraticheMockup();
        mainCard.Controls.Add(btnAggiornaPratiche);

        // 5. Pulsante Nuovo Invito Data Room (Allineato perfettamente a Y=56)
        btnCondividiPratica = new MaterialButton
        {
            Text = "Nuovo Invito Data Room",
            Location = new Point(mainCard.Width - 235, 56), // Distanziato dal tasto aggiorna
            Size = new Size(215, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Type = MaterialButton.MaterialButtonType.Contained,
            HighEmphasis = true,
            Enabled = false
        };
        btnCondividiPratica.Click += BtnCondividiPratica_Click;
        mainCard.Controls.Add(btnCondividiPratica);

        listViewStoricoPratiche = new MaterialListView
        {
            Location = new Point(14, 115),
            Size = new Size(mainCard.Width - 28, mainCard.Height - 140),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            FullRowSelect = true,
            View = View.Details
        };
        listViewStoricoPratiche.Columns.Add("Nome Pratica / Cartella", 380, HorizontalAlignment.Left);
        listViewStoricoPratiche.Columns.Add("Percorso Document Library SharePoint", 480, HorizontalAlignment.Left);
        listViewStoricoPratiche.Columns.Add("Data Creazione", 180, HorizontalAlignment.Left);
        listViewStoricoPratiche.Columns.Add("Stato", 140, HorizontalAlignment.Left);

        listViewStoricoPratiche.SelectedIndexChanged += ListViewStoricoPratiche_SelectedIndexChanged;
        mainCard.Controls.Add(listViewStoricoPratiche);
    }

    private void ListViewStoricoPratiche_SelectedIndexChanged(object sender, EventArgs e)
    {
        btnCondividiPratica.Enabled = listViewStoricoPratiche.SelectedItems.Count > 0;
    }

    private void BtnCondividiPratica_Click(object sender, EventArgs e)
    {
        if (listViewStoricoPratiche.SelectedItems.Count == 0) return;

        string praticaSelezionata = listViewStoricoPratiche.SelectedItems[0].Text;

        using (var popup = new InviteModalForm(praticaSelezionata))
        {
            if (popup.ShowDialog(this) == DialogResult.OK)
            {
                var nuovoItem = new ListViewItem(popup.SelectedPractice);
                nuovoItem.SubItems.Add(popup.Email);
                nuovoItem.SubItems.Add(popup.Role);
                nuovoItem.SubItems.Add(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                nuovoItem.SubItems.Add(popup.HasExpiration ? popup.ExpirationDate.ToString("dd/MM/yyyy") : "Nessuna");
                nuovoItem.SubItems.Add(string.IsNullOrWhiteSpace(popup.Notes) ? "Segreteria" : popup.Notes);
                nuovoItem.SubItems.Add("⏳ Inviato (Attesa)");

                _tuttiGliInviti.Insert(0, nuovoItem);

                if (listViewInviti != null)
                {
                    listViewInviti.Items.Clear();
                    listViewInviti.Items.AddRange(_tuttiGliInviti.ToArray());
                }

                string dettaglioScadenza = popup.HasExpiration ? popup.ExpirationDate.ToString("dd/MM/yyyy") : "Permanente";
                UiLogger.Info($"[SUCCESSO] Invito Data Room spedito a '{popup.Email}' per la pratica '{praticaSelezionata}'. Scadenza: {dettaglioScadenza}");
            }
        }
    }

    private void CaricaStoricoPraticheMockup()
    {
        if (listViewStoricoPratiche == null) return;
        listViewStoricoPratiche.Items.Clear();
        _tutteLePratiche.Clear();

        string[] mockPratiche = {
            "Pratica Fallimento Alfa S.r.l.",
            "Acquisizione Societaria Beta Corp",
            "Contenzioso Immobiliare Ditta Rossi",
            "Due Diligence Internazionale Gamma SpA"
        };

        DateTime dataPartenza = DateTime.Now.AddDays(-20);
        for (int i = 0; i < mockPratiche.Length; i++)
        {
            var item = new ListViewItem(mockPratiche[i]);
            item.SubItems.Add($"/Shared Documents/{mockPratiche[i].Replace(" ", "-")}");
            item.SubItems.Add(dataPartenza.AddDays(i * 4).ToString("dd/MM/yyyy HH:mm"));
            item.SubItems.Add(i % 3 == 0 ? "🔒 Chiusa" : "🔓 Attiva");
            _tutteLePratiche.Add(item);
        }
        listViewStoricoPratiche.Items.AddRange(_tutteLePratiche.ToArray());
    }

    private void TxtCercaPratica_TextChanged(object sender, EventArgs e)
    {
        string filtro = txtCercaPratica.Text.ToLower().Trim();
        listViewStoricoPratiche.BeginUpdate();
        listViewStoricoPratiche.Items.Clear();

        if (string.IsNullOrWhiteSpace(filtro))
            listViewStoricoPratiche.Items.AddRange(_tutteLePratiche.ToArray());
        else
        {
            foreach (var item in _tutteLePratiche)
                if (item.Text.ToLower().Contains(filtro))
                    listViewStoricoPratiche.Items.Add((ListViewItem)item.Clone());
        }
        listViewStoricoPratiche.EndUpdate();
    }

    private void AggiungiPraticaAStorico(string nomePratica, string data)
    {
        var item = new ListViewItem(nomePratica);
        item.SubItems.Add($"/Shared Documents/{nomePratica}");
        item.SubItems.Add(data);
        item.SubItems.Add("🔓 Attiva");
        _tutteLePratiche.Insert(0, item);
        TxtCercaPratica_TextChanged(null, null);
    }

    // ==========================================
    // LOGICA SCHEDA CONDIVISIONI / INVITI (TAB 3)
    // ==========================================
    private void InizializzaTabCondivisioni()
    {
        MaterialCard mainCard = new MaterialCard
        {
            Location = new Point(14, 14),
            Size = new Size(tabCondivisioni.Width - 28, tabCondivisioni.Height - 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            Padding = new Padding(14)
        };
        tabCondivisioni.Controls.Add(mainCard);

        Label lblTitolo = new Label
        {
            Text = "Registro Inviti Spediti e Controllo Accessi Esterni",
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            ForeColor = Color.FromArgb(63, 87, 133),
            Location = new Point(14, 14),
            AutoSize = true
        };
        mainCard.Controls.Add(lblTitolo);

        listViewInviti = new MaterialListView
        {
            Location = new Point(14, 60),
            Size = new Size(mainCard.Width - 28, mainCard.Height - 80),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            FullRowSelect = true,
            View = View.Details
        };
        listViewInviti.Columns.Add("Pratica Collegata", 260, HorizontalAlignment.Left);
        listViewInviti.Columns.Add("Destinatario (Email)", 260, HorizontalAlignment.Left);
        listViewInviti.Columns.Add("Livello Permesso", 160, HorizontalAlignment.Left);
        listViewInviti.Columns.Add("Data Spedizione", 150, HorizontalAlignment.Left);
        listViewInviti.Columns.Add("Scadenza Accesso", 150, HorizontalAlignment.Left);
        listViewInviti.Columns.Add("Richiedente / Note", 220, HorizontalAlignment.Left);
        listViewInviti.Columns.Add("Stato Invito", 130, HorizontalAlignment.Left);

        mainCard.Controls.Add(listViewInviti);
    }

    private void CaricaInvitiMockup()
    {
        if (listViewInviti == null) return;
        listViewInviti.Items.Clear();
        _tuttiGliInviti.Clear();

        var invito1 = new ListViewItem("Pratica Fallimento Alfa S.r.l.");
        invito1.SubItems.Add("studio.legale@controparte.it");
        invito1.SubItems.Add("Lettore (Solo Vista)");
        invito1.SubItems.Add(DateTime.Now.AddDays(-2).ToString("dd/MM/yyyy HH:mm"));
        invito1.SubItems.Add("31/12/2026");
        invito1.SubItems.Add("Avv. Giovanardi");
        invito1.SubItems.Add("✔ Accettato");

        var invito2 = new ListViewItem("Acquisizione Societaria Beta Corp");
        invito2.SubItems.Add("ceo@azienda-partner.com");
        invito2.SubItems.Add("Editor (Scrittura)");
        invito2.SubItems.Add(DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy HH:mm"));
        invito2.SubItems.Add("Nessuna");
        invito2.SubItems.Add("Richiesta dr. Bianchi");
        invito2.SubItems.Add("✔ Accettato");

        _tuttiGliInviti.Add(invito1);
        _tuttiGliInviti.Add(invito2);

        listViewInviti.Items.AddRange(_tuttiGliInviti.ToArray());
    }

    // ==========================================
    // GRAFICA ICONE VETTORIALI PROCEDURALI
    // ==========================================
    private void SetupTabIcons()
    {
        Color iconColor = _materialSkinManager.ColorScheme.PrimaryColor;

        panelAvatarUtente.BackgroundImage = CreateVectorIcon("avatar", Color.Empty);
        panelAvatarUtente.BackgroundImageLayout = ImageLayout.Center;

        panelIconaSharePoint.BackgroundImage = CreateVectorIcon("sp-logo", Color.Empty);
        panelIconaSharePoint.BackgroundImageLayout = ImageLayout.Center;

        ImageList drawerIcons = new ImageList();
        drawerIcons.ImageSize = new Size(24, 24);
        drawerIcons.ColorDepth = ColorDepth.Depth32Bit;

        drawerIcons.Images.Add("dash", CreateVectorIcon("dash", iconColor));
        drawerIcons.Images.Add("prat", CreateVectorIcon("prat", iconColor));
        drawerIcons.Images.Add("cond", CreateVectorIcon("cond", iconColor));
        drawerIcons.Images.Add("rice", CreateVectorIcon("rice", iconColor));
        drawerIcons.Images.Add("perm", CreateVectorIcon("perm", iconColor));
        drawerIcons.Images.Add("atti", CreateVectorIcon("atti", iconColor));
        drawerIcons.Images.Add("impo", CreateVectorIcon("impo", iconColor));

        materialTabControl1.ImageList = drawerIcons;

        tabDashboard.ImageKey = "dash";
        tabPratiche.ImageKey = "prat";
        tabCondivisioni.ImageKey = "cond";
        tabRicerca.ImageKey = "rice";
        tabPermessi.ImageKey = "perm";
        tabAttivita.ImageKey = "atti";
        tabImpostazioni.ImageKey = "impo";

        btnRiconnetti.Icon = CreateVectorIcon("refresh", iconColor);
        btnCreaCartella.Icon = CreateVectorIcon("plus-folder", Color.White);
        btnAggiungiInterno.Icon = CreateVectorIcon("plus", iconColor);
        btnModificaRuoloInterno.Icon = CreateVectorIcon("edit", iconColor);
        btnRimuoviInterno.Icon = CreateVectorIcon("trash", iconColor);
        btnAggiungiEsterno.Icon = CreateVectorIcon("plus", iconColor);
        btnModificaRuoloEsterno.Icon = CreateVectorIcon("edit", iconColor);
        btnRevocaEsterno.Icon = CreateVectorIcon("trash", iconColor);
        btnCondividiSelezionati.Icon = CreateVectorIcon("cond", Color.White);
        btnRevocaSelezionati.Icon = CreateVectorIcon("trash", iconColor);
        btnPulisciLog.Icon = CreateVectorIcon("trash", iconColor);
    }

    private Bitmap CreateVectorIcon(string type, Color color)
    {
        int size = (type == "avatar" || type == "sp-logo") ? 50 : 24;
        Bitmap bmp = new Bitmap(size, size);

        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using (Pen pen = new Pen(color, 2f))
            using (Brush brush = new SolidBrush(color))
            {
                switch (type.ToLower())
                {
                    case "avatar":
                        using (Brush bgUser = new SolidBrush(Color.FromArgb(220, 235, 253))) g.FillEllipse(bgUser, 1, 1, 48, 48);
                        using (Brush iconUser = new SolidBrush(Color.FromArgb(0, 120, 212)))
                        {
                            g.FillEllipse(iconUser, 19, 12, 12, 12);
                            g.FillPie(iconUser, 11, 25, 28, 22, 180, 180);
                        }
                        break;
                    case "sp-logo":
                        using (Brush bgSp = new SolidBrush(Color.FromArgb(16, 124, 65))) g.FillRectangle(bgSp, 1, 1, 48, 48);
                        using (Brush whiteBrush = new SolidBrush(Color.White))
                        {
                            g.FillRectangle(whiteBrush, 11, 11, 12, 12);
                            g.FillRectangle(whiteBrush, 27, 11, 12, 12);
                            g.FillRectangle(whiteBrush, 11, 27, 12, 12);
                            using (Brush alphaWhite = new SolidBrush(Color.FromArgb(180, 255, 255, 255))) g.FillRectangle(alphaWhite, 27, 27, 12, 12);
                        }
                        break;
                    case "dash":
                        g.DrawPolygon(pen, new Point[] { new Point(12, 3), new Point(4, 11), new Point(20, 11) });
                        g.DrawRectangle(pen, 7, 11, 10, 9);
                        break;
                    case "prat":
                        g.DrawRectangle(pen, 3, 7, 18, 12);
                        g.FillRectangle(brush, 4, 4, 6, 4);
                        break;
                    case "cond":
                        g.FillEllipse(brush, 3, 10, 5, 5); g.FillEllipse(brush, 15, 4, 5, 5); g.FillEllipse(brush, 15, 16, 5, 5);
                        g.DrawLine(pen, 7, 12, 15, 7); g.DrawLine(pen, 7, 13, 15, 18);
                        break;
                    case "rice":
                        g.DrawEllipse(pen, 4, 4, 10, 10); g.DrawLine(pen, 13, 13, 19, 19);
                        break;
                    case "perm":
                        g.DrawRectangle(pen, 5, 10, 14, 10); g.DrawArc(pen, 8, 4, 8, 12, 180, 180);
                        break;
                    case "atti":
                        g.DrawEllipse(pen, 3, 3, 18, 18); g.DrawLine(pen, 12, 12, 12, 7); g.DrawLine(pen, 12, 12, 16, 12);
                        break;
                    case "impo":
                        g.DrawEllipse(pen, 7, 7, 10, 10); g.DrawEllipse(pen, 11, 11, 2, 2);
                        for (int i = 0; i < 360; i += 45)
                        {
                            double rad = i * Math.PI / 180;
                            g.DrawLine(pen, (int)(12 + 5 * Math.Cos(rad)), (int)(12 + 5 * Math.Sin(rad)), (int)(12 + 8 * Math.Cos(rad)), (int)(12 + 8 * Math.Sin(rad)));
                        }
                        break;
                    case "refresh":
                        g.DrawArc(pen, 4, 4, 16, 16, -45, 280); g.FillPolygon(brush, new Point[] { new Point(18, 5), new Point(22, 10), new Point(14, 10) });
                        break;
                    case "plus":
                        g.DrawLine(pen, 12, 5, 12, 19); g.DrawLine(pen, 5, 12, 19, 12);
                        break;
                    case "plus-folder":
                        g.DrawRectangle(pen, 3, 8, 14, 11); g.FillRectangle(brush, 4, 5, 5, 4);
                        g.DrawLine(pen, 19, 13, 19, 21); g.DrawLine(pen, 15, 17, 23, 17);
                        break;
                    case "edit":
                        g.DrawRectangle(pen, 4, 16, 16, 4); g.DrawLine(pen, 6, 16, 16, 5); g.DrawLine(pen, 8, 16, 18, 5);
                        break;
                    case "trash":
                        g.DrawLine(pen, 4, 5, 20, 5); g.DrawRectangle(pen, 6, 5, 12, 15);
                        g.DrawLine(pen, 10, 8, 10, 16); g.DrawLine(pen, 14, 8, 14, 16);
                        break;
                }
            }
        }
        return bmp;
    }
}

// ============================================================================
// CLASSE COMPLEMENTARE: FORM POP-UP MODALE CON CALENDARIO E NOTE
// ============================================================================
public class InviteModalForm : MaterialForm
{
    private MaterialComboBox cmbPratiche;
    private MaterialTextBox2 txtEmail;
    private MaterialComboBox cmbRuolo;
    private MaterialSwitch switchScadenza;
    private DateTimePicker dtpScadenza;
    private MaterialTextBox2 txtNoteRichiedente;
    private MaterialButton btnInvia;
    private MaterialButton btnAnnulla;

    public string SelectedPractice => cmbPratiche.Items.Count > 0 ? cmbPratiche.Items[0].ToString() : "";
    public string Email => txtEmail.Text.Trim();
    public string Role => cmbRuolo.SelectedItem?.ToString() ?? "";
    public bool HasExpiration => switchScadenza.Checked;
    public DateTime ExpirationDate => dtpScadenza.Value;
    public string Notes => txtNoteRichiedente.Text.Trim();

    public InviteModalForm(string praticaSelezionata)
    {
        this.Size = new Size(460, 540);
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Text = "Nuovo Invito Data Room";

        var manager = MaterialSkinManager.Instance;
        manager.AddFormToManage(this);

        cmbPratiche = new MaterialComboBox
        {
            Hint = "Pratica / Cartella Associata",
            Location = new Point(20, 80),
            Size = new Size(420, 48),
            Enabled = false
        };
        cmbPratiche.Items.Add(praticaSelezionata);
        cmbPratiche.SelectedIndex = 0;
        this.Controls.Add(cmbPratiche);

        txtEmail = new MaterialTextBox2
        {
            Hint = "Indirizzo Email dell'ospite",
            Location = new Point(20, 145),
            Size = new Size(420, 48)
        };
        this.Controls.Add(txtEmail);

        cmbRuolo = new MaterialComboBox
        {
            Hint = "Livello di Permesso / Ruolo",
            Location = new Point(20, 210),
            Size = new Size(420, 48)
        };
        cmbRuolo.Items.Add("Lettore (Solo Vista)");
        cmbRuolo.Items.Add("Editor (Scrittura)");
        cmbRuolo.SelectedIndex = 0;
        this.Controls.Add(cmbRuolo);

        switchScadenza = new MaterialSwitch
        {
            Text = "Imposta data di scadenza",
            Location = new Point(20, 275),
            Size = new Size(230, 30),
            Checked = false
        };
        this.Controls.Add(switchScadenza);

        dtpScadenza = new DateTimePicker
        {
            Location = new Point(250, 277),
            Size = new Size(190, 25),
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Now.AddDays(30),
            Enabled = false
        };
        this.Controls.Add(dtpScadenza);

        switchScadenza.CheckedChanged += (s, e) => {
            dtpScadenza.Enabled = switchScadenza.Checked;
        };

        txtNoteRichiedente = new MaterialTextBox2
        {
            Hint = "Avvocato Richiedente / Note Interne",
            Location = new Point(20, 335),
            Size = new Size(420, 48)
        };
        this.Controls.Add(txtNoteRichiedente);

        btnAnnulla = new MaterialButton
        {
            Text = "Annulla",
            Location = new Point(230, 440),
            Size = new Size(90, 36),
            Type = MaterialButton.MaterialButtonType.Text
        };
        btnAnnulla.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        this.Controls.Add(btnAnnulla);

        btnInvia = new MaterialButton
        {
            Text = "Invia Invito",
            Location = new Point(330, 440),
            Size = new Size(110, 36),
            Type = MaterialButton.MaterialButtonType.Contained,
            HighEmphasis = true
        };
        btnInvia.Click += BtnInvia_Click;
        this.Controls.Add(btnInvia);
    }

    private void BtnInvia_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
        {
            MaterialMessageBox.Show("Inserisci un indirizzo email valido per l'ospite.", "Validazione fallita", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void ForzaLayoutListe()
    {
        // Cerchiamo i controlli per nome, così evitiamo errori di "variabile non trovata"
        Control[] tabs = this.Controls.Find("tabDashboard", true);
        Control[] internis = this.Controls.Find("listViewInterni", true);
        Control[] esternis = this.Controls.Find("listViewEsterni", true);

        if (tabs.Length > 0 && internis.Length > 0 && esternis.Length > 0)
        {
            TabPage tab = (TabPage)tabs[0];
            ListView listInt = (ListView)internis[0];
            ListView listEst = (ListView)esternis[0];

            // Rimuoviamo il Dock, altrimenti il designer lo resetta male
            listInt.Dock = DockStyle.None;
            listEst.Dock = DockStyle.None;

            // Calcoliamo lo spazio
            int padding = 10;
            int metaLarghezza = (tab.Width / 2) - (padding * 2);

            // Posizioniamo quella di SINISTRA
            listInt.Location = new Point(padding, 50);
            listInt.Size = new Size(metaLarghezza, tab.Height - 70);

            // Posizioniamo quella di DESTRA
            listEst.Location = new Point(metaLarghezza + (padding * 2), 50);
            listEst.Size = new Size(metaLarghezza, tab.Height - 70);
        }
    }
}