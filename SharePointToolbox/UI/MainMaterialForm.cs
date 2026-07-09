using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.Models;
using SharePointToolbox.Helpers;

namespace SharePointToolbox.UI;

public partial class MainMaterialForm : MaterialForm
{
    // ==========================================
    // SERVIZI E DIPENDENZE (BACKEND ESISTENTE)
    // ==========================================
    private readonly AuthenticationService _authenticationService;
    private readonly SharePointService _sharePointService;
    private readonly MaterialSkinManager _materialSkinManager;

    public MainMaterialForm(
        AuthenticationService authenticationService,
        SharePointService sharePointService)
    {
        InitializeComponent();

        _authenticationService = authenticationService;
        _sharePointService = sharePointService;

        // Configurazione Tema Grafico MaterialSkin2
        _materialSkinManager = MaterialSkinManager.Instance;
        _materialSkinManager.AddFormToManage(this);
        _materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;

        // Palette Colori Blu Istituzionale (Stile SharePoint / M365)
        _materialSkinManager.ColorScheme = new ColorScheme(
            Primary.Blue900,       // Barra del titolo principale
            Primary.Blue800,       // Barra laterale attiva / Sfondi scuri
            Primary.Blue500,       // Linee di focus e selezioni leggere
            Accent.Blue400,        // Accento per controlli attivi e toggle
            TextShade.WHITE        // Colore del testo sulle barre scure
        );

        // Opzioni Menu Laterale Navigazione (Drawer)
        this.DrawerShowIconsWhenHidden = true;
        this.DrawerUseColors = true;
        this.DrawerWidth = 240;

        // Inizializza il logger condiviso reindirizzandolo sulla nostra ListView dei Log visiva
        UiLogger.Initialize(AddLogLine);

        // Inserimento icone
        SetupTabIcons();
    }

    private void MainMaterialForm_Load(object sender, EventArgs e)
    {
        // Setup iniziale dei dati grafici (Stato disconnesso/Pronto)
        ResetInterfacciaGrafica();
        UiLogger.Info("Applicazione SharePoint Toolbox avviata. Pronto per il login.");
    }

    private void ResetInterfacciaGrafica()
    {
        // Stato iniziale dei testi
        lblUtenteNome.Text = "Non autenticato";
        lblUtenteEmail.Text = "Effettua il login per iniziare";
        lblSitoNome.Text = "-";
        lblSitoUrl.Text = "Nessun sito SharePoint caricato";

        lblStatusConnesso.Text = "Disconnesso";
        lblStatusConnesso.ForeColor = Color.Red;
        lblStatusUtente.Text = "Utente: -";
        lblStatusTenant.Text = "Tenant: -";
        lblStatusSito.Text = "Sito: -";

        // Disabilita azioni fino al login avvenuto
        btnCreaCartella.Enabled = false;
        txtNomePratica.Enabled = false;

        // Configurazione colonne tabelle/ListView
        listViewLog.Columns.Clear();
        listViewLog.Columns.Add("", 30, HorizontalAlignment.Center); // Icona stato
        listViewLog.Columns.Add("Orario", 140, HorizontalAlignment.Left);
        listViewLog.Columns.Add("Messaggio di Registro", 800, HorizontalAlignment.Left);

        listViewInterni.Columns.Clear();
        listViewInterni.Columns.Add("Nome", 180, HorizontalAlignment.Left);
        listViewInterni.Columns.Add("Ruolo", 100, HorizontalAlignment.Left);
        listViewInterni.Columns.Add("Tipo", 100, HorizontalAlignment.Left);

        listViewEsterni.Columns.Clear();
        listViewEsterni.Columns.Add("Nome / Email", 200, HorizontalAlignment.Left);
        listViewEsterni.Columns.Add("Ruolo", 100, HorizontalAlignment.Left);
        listViewEsterni.Columns.Add("Tipo", 100, HorizontalAlignment.Left);
    }

    // ==========================================
    // AZIONI DI INTERFACCIA ED EVENTI ASINCRONI
    // ==========================================

    private async void btnRiconnetti_Click(object sender, EventArgs e)
    {
        UiLogger.Info("Connessione a Microsoft 365 in corso...");
        try
        {
            // Esegue il tuo login reale esistente
            var result = await _authenticationService.SignInAsync();

            // Aggiorna la UI con le info dell'account recuperato
            lblUtenteNome.Text = result.Account.Username.Split('@')[0].Replace(".", " "); 
            lblUtenteEmail.Text = result.Account.Username;

            UiLogger.Info($"Login effettuato con successo: {result.Account.Username}");

            // Recupera la libreria documenti di SharePoint usando il tuo servizio
            DocumentLibrary library = await _sharePointService.GetDefaultDocumentLibraryAsync();

            lblSitoNome.Text = library.Name;
            lblSitoUrl.Text = "Sito SharePoint connesso correttamente.";
            UiLogger.Info($"Sito SharePoint agganciato: {library.Name}");

            // Aggiorna la barra di stato in fondo alla finestra
            lblStatusConnesso.Text = "✔ Connesso";
            lblStatusConnesso.ForeColor = Color.Green;
            lblStatusUtente.Text = $"Utente: {result.Account.Username}";
            lblStatusTenant.Text = $"Tenant: {result.TenantId ?? "Aziendale"}";
            lblStatusSito.Text = $"Sito: {library.Name}";

            // Abilita i controlli della pratica
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
        string folderName = txtNomePratica.Text.Trim();

        if (string.IsNullOrWhiteSpace(folderName))
        {
            MessageBox.Show("Inserisci il nome della pratica prima di procedere.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UiLogger.Info($"Creazione della cartella pratica '{folderName}' su SharePoint...");
        try
        {
            // Richiama il tuo servizio SharePoint reale per creare la cartella
            await _sharePointService.CreateFolderAsync(folderName);
            UiLogger.Info($"[SUCCESSO] Cartella creata correttamente: {folderName}");

            // Popola i dettagli della card di destra "Cartella Selezionata" come da mockup
            lblDettaglioPercorso.Text = $"/Documenti/{folderName}";
            lblDettaglioID.Text = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(); // ID Generato di riferimento
            lblDettaglioData.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            lblDettaglioCreatore.Text = lblUtenteNome.Text;

            lblNoCartella.Visible = false;
        }
        catch (Exception ex)
        {
            UiLogger.Info($"[ERRORE] Impossibile creare la cartella: {ex.Message}");
            MessageBox.Show(ex.Message, "Errore SharePoint", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // INTERCETTAZIONE LOG: Converte le stringhe del tuo UiLogger in righe grafiche della ListView
    private void AddLogLine(string text)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action<string>(AddLogLine), text);
            return;
        }

        string timestamp = DateTime.Now.ToString("25/06/2026  HH:mm:ss"); // Forzato anno mockup o DateTime.Now
        string icona = "ℹ";
        
        if (text.Contains("[SUCCESSO]")) icona = "✔";
        if (text.Contains("[ERRORE]")) icona = "❌";

        var item = new ListViewItem(icona);
        item.SubItems.Add(timestamp);
        item.SubItems.Add(text.Replace("[SUCCESSO]", "").Replace("[ERRORE]", ""));
        
        listViewLog.Items.Add(item);
        listViewLog.EnsureVisible(listViewLog.Items.Count - 1); // Auto-scroll all'ultimo log

        lblStatusAggiornamento.Text = $"Ultimo aggiornamento: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}";
    }

    private void btnPulisciLog_Click(object sender, EventArgs e)
    {
        listViewLog.Items.Clear();
    }
    private void SetupTabIcons()
    {
        Color iconColor = _materialSkinManager.ColorScheme.PrimaryColor;

        // 1. ASSEGNAZIONE DELLE ICONE AI DUE PANNELLI SUPERIORI (NOVITÀ MOCKUP)
        panelAvatarUtente.BackgroundImage = CreateVectorIcon("avatar", Color.Empty);
        panelAvatarUtente.BackgroundImageLayout = ImageLayout.Center;

        panelIconaSharePoint.BackgroundImage = CreateVectorIcon("sp-logo", Color.Empty);
        panelIconaSharePoint.BackgroundImageLayout = ImageLayout.Center;

        // 2. CONFIGURAZIONE ICONE MENU LATERALE (DRAWER)
        System.Windows.Forms.ImageList drawerIcons = new System.Windows.Forms.ImageList();
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

        // 3. APPLICAZIONE ICONE AI PULSANTI
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

    // Funzione di supporto per disegnare le icone segnaposto al volo senza file esterni
    // Motore grafico procedurale per la generazione di icone e loghi reali
    private Bitmap CreateVectorIcon(string type, Color color)
    {
        // I pannelli in alto sono 50x50, le icone dei tasti/menu sono 24x24
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
                    case "avatar": // Icona Profilo Utente in Alto a Sinistra (50x50)
                        using (Brush bgUser = new SolidBrush(Color.FromArgb(220, 235, 253)))
                        {
                            g.FillEllipse(bgUser, 1, 1, 48, 48); // Cerchio di sfondo azzurro chiaro
                        }
                        using (Brush iconUser = new SolidBrush(Color.FromArgb(0, 120, 212)))
                        {
                            g.FillEllipse(iconUser, 19, 12, 12, 12); // Testa sagoma
                            g.FillPie(iconUser, 11, 25, 28, 22, 180, 180); // Spalle sagoma
                        }
                        break;

                    case "sp-logo": // Logo SharePoint in Alto a Destra (50x50)
                        using (Brush bgSp = new SolidBrush(Color.FromArgb(16, 124, 65)))
                        {
                            g.FillRectangle(bgSp, 1, 1, 48, 48); // Riquadro Verde SharePoint corporativo
                        }
                        using (Brush whiteBrush = new SolidBrush(Color.White))
                        {
                            // Disegna la composizione geometrica dei quadranti tipici dell'ecosistema Microsoft List/SharePoint
                            g.FillRectangle(whiteBrush, 11, 11, 12, 12);
                            g.FillRectangle(whiteBrush, 27, 11, 12, 12);
                            g.FillRectangle(whiteBrush, 11, 27, 12, 12);
                            using (Brush alphaWhite = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                            {
                                g.FillRectangle(alphaWhite, 27, 27, 12, 12); // Angolo con trasparenza
                            }
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
                        g.FillEllipse(brush, 3, 10, 5, 5);
                        g.FillEllipse(brush, 15, 4, 5, 5);
                        g.FillEllipse(brush, 15, 16, 5, 5);
                        g.DrawLine(pen, 7, 12, 15, 7);
                        g.DrawLine(pen, 7, 13, 15, 18);
                        break;

                    case "rice":
                        g.DrawEllipse(pen, 4, 4, 10, 10);
                        g.DrawLine(pen, 13, 13, 19, 19);
                        break;

                    case "perm":
                        g.DrawRectangle(pen, 5, 10, 14, 10);
                        g.DrawArc(pen, 8, 4, 8, 12, 180, 180);
                        break;

                    case "atti":
                        g.DrawEllipse(pen, 3, 3, 18, 18);
                        g.DrawLine(pen, 12, 12, 12, 7);
                        g.DrawLine(pen, 12, 12, 16, 12);
                        break;

                    case "impo":
                        g.DrawEllipse(pen, 7, 7, 10, 10);
                        g.DrawEllipse(pen, 11, 11, 2, 2);
                        for (int i = 0; i < 360; i += 45)
                        {
                            double rad = i * Math.PI / 180;
                            int x1 = (int)(12 + 5 * Math.Cos(rad));
                            int y1 = (int)(12 + 5 * Math.Sin(rad));
                            int x2 = (int)(12 + 8 * Math.Cos(rad));
                            int y2 = (int)(12 + 8 * Math.Sin(rad));
                            g.DrawLine(pen, x1, y1, x2, y2);
                        }
                        break;

                    case "refresh":
                        g.DrawArc(pen, 4, 4, 16, 16, -45, 280);
                        g.FillPolygon(brush, new Point[] { new Point(18, 5), new Point(22, 10), new Point(14, 10) });
                        break;

                    case "plus":
                        g.DrawLine(pen, 12, 5, 12, 19);
                        g.DrawLine(pen, 5, 12, 19, 12);
                        break;

                    case "plus-folder":
                        g.DrawRectangle(pen, 3, 8, 14, 11);
                        g.FillRectangle(brush, 4, 5, 5, 4);
                        g.DrawLine(pen, 19, 13, 19, 21);
                        g.DrawLine(pen, 15, 17, 23, 17);
                        break;

                    case "edit":
                        g.DrawRectangle(pen, 4, 16, 16, 4);
                        g.DrawLine(pen, 6, 16, 16, 5);
                        g.DrawLine(pen, 8, 16, 18, 5);
                        break;

                    case "trash":
                        g.DrawLine(pen, 4, 5, 20, 5);
                        g.DrawRectangle(pen, 6, 5, 12, 15);
                        g.DrawLine(pen, 10, 8, 10, 16);
                        g.DrawLine(pen, 14, 8, 14, 16);
                        break;
                }
            }
        }
        return bmp;
    }
}