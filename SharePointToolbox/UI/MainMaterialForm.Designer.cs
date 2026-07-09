using System.Windows.Forms;
using System.Drawing;

namespace SharePointToolbox.UI
{
    partial class MainMaterialForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            materialTabControl1 = new MaterialSkin.Controls.MaterialTabControl();
            tabDashboard = new TabPage();
            cardUtente = new MaterialSkin.Controls.MaterialCard();
            lblUtenteEmail = new Label();
            lblUtenteNome = new Label();
            lblUtenteHeader = new Label();
            panelAvatarUtente = new Panel();
            cardSharePoint = new MaterialSkin.Controls.MaterialCard();
            btnRiconnetti = new MaterialSkin.Controls.MaterialButton();
            lblSitoUrl = new Label();
            lblSitoNome = new Label();
            lblSharePointHeader = new Label();
            panelIconaSharePoint = new Panel();
            cardPratica = new MaterialSkin.Controls.MaterialCard();
            btnCreaCartella = new MaterialSkin.Controls.MaterialButton();
            txtNomePratica = new MaterialSkin.Controls.MaterialTextBox2();
            lblNomePraticaLabel = new Label();
            lblPraticaHeader = new Label();
            cardCondivisioniMain = new MaterialSkin.Controls.MaterialCard();
            btnRevocaSelezionati = new MaterialSkin.Controls.MaterialButton();
            btnCondividiSelezionati = new MaterialSkin.Controls.MaterialButton();
            cardDettagliCartella = new Panel();
            lblDettaglioCreatore = new Label();
            lblDettaglioData = new Label();
            lblDettaglioID = new Label();
            lblDettaglioPercorso = new Label();
            lblCreataDa = new Label();
            lblCreataIl = new Label();
            lblIdCartella = new Label();
            lblPercorso = new Label();
            lblNoCartella = new Label();
            lblCartellaSelezionataHeader = new Label();
            cardEsterni = new MaterialSkin.Controls.MaterialCard();
            btnRevocaEsterno = new MaterialSkin.Controls.MaterialButton();
            btnModificaRuoloEsterno = new MaterialSkin.Controls.MaterialButton();
            btnAggiungiEsterno = new MaterialSkin.Controls.MaterialButton();
            lblNoEsterni = new Label();
            listViewEsterni = new MaterialSkin.Controls.MaterialListView();
            lblEsterniHeader = new Label();
            cardInterni = new MaterialSkin.Controls.MaterialCard();
            btnRimuoviInterno = new MaterialSkin.Controls.MaterialButton();
            btnModificaRuoloInterno = new MaterialSkin.Controls.MaterialButton();
            btnAggiungiInterno = new MaterialSkin.Controls.MaterialButton();
            lblNoInterni = new Label();
            listViewInterni = new MaterialSkin.Controls.MaterialListView();
            lblInterniHeader = new Label();
            lblCondivisioniHeader = new Label();
            cardLog = new MaterialSkin.Controls.MaterialCard();
            btnPulisciLog = new MaterialSkin.Controls.MaterialButton();
            listViewLog = new MaterialSkin.Controls.MaterialListView();
            lblLogHeader = new Label();
            tabPratiche = new TabPage();
            tabCondivisioni = new TabPage();
            tabRicerca = new TabPage();
            tabPermessi = new TabPage();
            tabAttivita = new TabPage();
            tabImpostazioni = new TabPage();
            panelStatus = new Panel();
            lblStatusAggiornamento = new Label();
            lblStatusVersione = new Label();
            lblStatusSito = new Label();
            lblStatusTenant = new Label();
            lblStatusUtente = new Label();
            lblStatusConnesso = new Label();
            materialTabControl1.SuspendLayout();
            tabDashboard.SuspendLayout();
            cardUtente.SuspendLayout();
            cardSharePoint.SuspendLayout();
            cardPratica.SuspendLayout();
            cardCondivisioniMain.SuspendLayout();
            cardDettagliCartella.SuspendLayout();
            cardEsterni.SuspendLayout();
            cardInterni.SuspendLayout();
            cardLog.SuspendLayout();
            panelStatus.SuspendLayout();
            SuspendLayout();
            // 
            // materialTabControl1
            // 
            materialTabControl1.Controls.Add(tabDashboard);
            materialTabControl1.Controls.Add(tabPratiche);
            materialTabControl1.Controls.Add(tabCondivisioni);
            materialTabControl1.Controls.Add(tabRicerca);
            materialTabControl1.Controls.Add(tabPermessi);
            materialTabControl1.Controls.Add(tabAttivita);
            materialTabControl1.Controls.Add(tabImpostazioni);
            materialTabControl1.Depth = 0;
            materialTabControl1.Dock = DockStyle.Fill;
            materialTabControl1.Location = new Point(3, 64);
            materialTabControl1.MouseState = MaterialSkin.MouseState.HOVER;
            materialTabControl1.Multiline = true;
            materialTabControl1.Name = "materialTabControl1";
            materialTabControl1.SelectedIndex = 0;
            materialTabControl1.Size = new Size(1494, 882);
            materialTabControl1.TabIndex = 0;
            // 
            // tabDashboard
            // 
            tabDashboard.BackColor = Color.FromArgb(244, 246, 249);
            tabDashboard.Controls.Add(cardUtente);
            tabDashboard.Controls.Add(cardSharePoint);
            tabDashboard.Controls.Add(cardPratica);
            tabDashboard.Controls.Add(cardCondivisioniMain);
            tabDashboard.Controls.Add(cardLog);
            tabDashboard.Location = new Point(4, 24);
            tabDashboard.Name = "tabDashboard";
            tabDashboard.Padding = new Padding(10);
            tabDashboard.Size = new Size(1486, 854);
            tabDashboard.TabIndex = 0;
            tabDashboard.Text = "Dashboard";
            // 
            // cardUtente
            // 
            cardUtente.BackColor = Color.FromArgb(255, 255, 255);
            cardUtente.Controls.Add(lblUtenteEmail);
            cardUtente.Controls.Add(lblUtenteNome);
            cardUtente.Controls.Add(lblUtenteHeader);
            cardUtente.Controls.Add(panelAvatarUtente);
            cardUtente.Depth = 0;
            cardUtente.ForeColor = Color.FromArgb(222, 0, 0, 0);
            cardUtente.Location = new Point(14, 10);
            cardUtente.Margin = new Padding(14);
            cardUtente.MouseState = MaterialSkin.MouseState.HOVER;
            cardUtente.Name = "cardUtente";
            cardUtente.Padding = new Padding(14);
            cardUtente.Size = new Size(440, 106);
            cardUtente.TabIndex = 0;
            // 
            // lblUtenteEmail
            // 
            lblUtenteEmail.AutoSize = true;
            lblUtenteEmail.Font = new Font("Segoe UI", 9F);
            lblUtenteEmail.ForeColor = Color.Gray;
            lblUtenteEmail.Location = new Point(82, 63);
            lblUtenteEmail.Name = "lblUtenteEmail";
            lblUtenteEmail.Size = new Size(36, 15);
            lblUtenteEmail.TabIndex = 3;
            lblUtenteEmail.Text = "email";
            // 
            // lblUtenteNome
            // 
            lblUtenteNome.AutoSize = true;
            lblUtenteNome.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblUtenteNome.ForeColor = Color.FromArgb(30, 30, 30);
            lblUtenteNome.Location = new Point(81, 40);
            lblUtenteNome.Name = "lblUtenteNome";
            lblUtenteNome.Size = new Size(51, 20);
            lblUtenteNome.TabIndex = 2;
            lblUtenteNome.Text = "Nome";
            // 
            // lblUtenteHeader
            // 
            lblUtenteHeader.AutoSize = true;
            lblUtenteHeader.Font = new Font("Segoe UI", 8.25F);
            lblUtenteHeader.ForeColor = Color.DarkSlateGray;
            lblUtenteHeader.Location = new Point(82, 18);
            lblUtenteHeader.Name = "lblUtenteHeader";
            lblUtenteHeader.Size = new Size(42, 13);
            lblUtenteHeader.TabIndex = 1;
            lblUtenteHeader.Text = "Utente";
            // 
            // panelAvatarUtente
            // 
            panelAvatarUtente.BackColor = Color.Transparent;
            panelAvatarUtente.Location = new Point(17, 18);
            panelAvatarUtente.Name = "panelAvatarUtente";
            panelAvatarUtente.Size = new Size(50, 50);
            panelAvatarUtente.TabIndex = 0;
            // 
            // cardSharePoint
            // 
            cardSharePoint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardSharePoint.BackColor = Color.FromArgb(255, 255, 255);
            cardSharePoint.Controls.Add(btnRiconnetti);
            cardSharePoint.Controls.Add(lblSitoUrl);
            cardSharePoint.Controls.Add(lblSitoNome);
            cardSharePoint.Controls.Add(lblSharePointHeader);
            cardSharePoint.Controls.Add(panelIconaSharePoint);
            cardSharePoint.Depth = 0;
            cardSharePoint.ForeColor = Color.FromArgb(222, 0, 0, 0);
            cardSharePoint.Location = new Point(468, 10);
            cardSharePoint.Margin = new Padding(14);
            cardSharePoint.MouseState = MaterialSkin.MouseState.HOVER;
            cardSharePoint.Name = "cardSharePoint";
            cardSharePoint.Padding = new Padding(14);
            cardSharePoint.Size = new Size(1004, 106);
            cardSharePoint.TabIndex = 1;
            // 
            // btnRiconnetti
            // 
            btnRiconnetti.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRiconnetti.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRiconnetti.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnRiconnetti.Depth = 0;
            btnRiconnetti.HighEmphasis = false;
            btnRiconnetti.Icon = null;
            btnRiconnetti.Location = new Point(879, 33);
            btnRiconnetti.Margin = new Padding(4, 6, 4, 6);
            btnRiconnetti.MouseState = MaterialSkin.MouseState.HOVER;
            btnRiconnetti.Name = "btnRiconnetti";
            btnRiconnetti.NoAccentTextColor = Color.Empty;
            btnRiconnetti.Size = new Size(106, 36);
            btnRiconnetti.TabIndex = 4;
            btnRiconnetti.Text = "Riconnetti";
            btnRiconnetti.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Outlined;
            btnRiconnetti.UseAccentColor = false;
            btnRiconnetti.Click += btnRiconnetti_Click;
            // 
            // lblSitoUrl
            // 
            lblSitoUrl.AutoSize = true;
            lblSitoUrl.Font = new Font("Segoe UI", 9F);
            lblSitoUrl.ForeColor = Color.Gray;
            lblSitoUrl.Location = new Point(82, 63);
            lblSitoUrl.Name = "lblSitoUrl";
            lblSitoUrl.Size = new Size(21, 15);
            lblSitoUrl.TabIndex = 3;
            lblSitoUrl.Text = "url";
            // 
            // lblSitoNome
            // 
            lblSitoNome.AutoSize = true;
            lblSitoNome.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            lblSitoNome.ForeColor = Color.FromArgb(30, 30, 30);
            lblSitoNome.Location = new Point(81, 40);
            lblSitoNome.Name = "lblSitoNome";
            lblSitoNome.Size = new Size(81, 20);
            lblSitoNome.TabIndex = 2;
            lblSitoNome.Text = "Sito Nome";
            // 
            // lblSharePointHeader
            // 
            lblSharePointHeader.AutoSize = true;
            lblSharePointHeader.Font = new Font("Segoe UI", 8.25F);
            lblSharePointHeader.ForeColor = Color.DarkSlateGray;
            lblSharePointHeader.Location = new Point(82, 18);
            lblSharePointHeader.Name = "lblSharePointHeader";
            lblSharePointHeader.Size = new Size(86, 13);
            lblSharePointHeader.TabIndex = 1;
            lblSharePointHeader.Text = "Sito SharePoint";
            // 
            // panelIconaSharePoint
            // 
            panelIconaSharePoint.BackColor = Color.Transparent;
            panelIconaSharePoint.Location = new Point(17, 18);
            panelIconaSharePoint.Name = "panelIconaSharePoint";
            panelIconaSharePoint.Size = new Size(50, 50);
            panelIconaSharePoint.TabIndex = 0;
            // 
            // cardPratica
            // 
            cardPratica.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardPratica.BackColor = Color.FromArgb(255, 255, 255);
            cardPratica.Controls.Add(btnCreaCartella);
            cardPratica.Controls.Add(txtNomePratica);
            cardPratica.Controls.Add(lblNomePraticaLabel);
            cardPratica.Controls.Add(lblPraticaHeader);
            cardPratica.Depth = 0;
            cardPratica.ForeColor = Color.FromArgb(222, 0, 0, 0);
            cardPratica.Location = new Point(14, 130);
            cardPratica.Margin = new Padding(14);
            cardPratica.MouseState = MaterialSkin.MouseState.HOVER;
            cardPratica.Name = "cardPratica";
            cardPratica.Padding = new Padding(14);
            cardPratica.Size = new Size(1458, 110);
            cardPratica.TabIndex = 2;
            // 
            // btnCreaCartella
            // 
            btnCreaCartella.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCreaCartella.AutoSize = false;
            btnCreaCartella.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCreaCartella.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnCreaCartella.Depth = 0;
            btnCreaCartella.HighEmphasis = true;
            btnCreaCartella.Icon = null;
            btnCreaCartella.Location = new Point(1270, 45);
            btnCreaCartella.Margin = new Padding(4, 6, 4, 6);
            btnCreaCartella.MouseState = MaterialSkin.MouseState.HOVER;
            btnCreaCartella.Name = "btnCreaCartella";
            btnCreaCartella.NoAccentTextColor = Color.Empty;
            btnCreaCartella.Size = new Size(170, 48);
            btnCreaCartella.TabIndex = 3;
            btnCreaCartella.Text = "Crea cartella";
            btnCreaCartella.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            btnCreaCartella.UseAccentColor = true;
            btnCreaCartella.Click += btnCreaCartella_Click;
            // 
            // txtNomePratica
            // 
            txtNomePratica.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtNomePratica.AnimateReadOnly = false;
            txtNomePratica.BackgroundImageLayout = ImageLayout.None;
            txtNomePratica.CharacterCasing = CharacterCasing.Normal;
            txtNomePratica.Depth = 0;
            txtNomePratica.Font = new Font("Segoe UI", 12F);
            txtNomePratica.HideSelection = true;
            txtNomePratica.Hint = "Inserisci il nome della pratica (testo libero)...";
            txtNomePratica.LeadingIcon = null;
            txtNomePratica.Location = new Point(145, 45);
            txtNomePratica.MaxLength = 32767;
            txtNomePratica.MouseState = MaterialSkin.MouseState.OUT;
            txtNomePratica.Name = "txtNomePratica";
            txtNomePratica.PasswordChar = '\0';
            txtNomePratica.PrefixSuffixText = null;
            txtNomePratica.ReadOnly = false;
            txtNomePratica.RightToLeft = RightToLeft.No;
            txtNomePratica.SelectedText = "";
            txtNomePratica.SelectionLength = 0;
            txtNomePratica.SelectionStart = 0;
            txtNomePratica.ShortcutsEnabled = true;
            txtNomePratica.Size = new Size(1105, 48);
            txtNomePratica.TabIndex = 2;
            txtNomePratica.TabStop = false;
            txtNomePratica.TextAlign = HorizontalAlignment.Left;
            txtNomePratica.TrailingIcon = null;
            txtNomePratica.UseSystemPasswordChar = false;
            // 
            // lblNomePraticaLabel
            // 
            lblNomePraticaLabel.AutoSize = true;
            lblNomePraticaLabel.Font = new Font("Segoe UI", 10F);
            lblNomePraticaLabel.Location = new Point(17, 58);
            lblNomePraticaLabel.Name = "lblNomePraticaLabel";
            lblNomePraticaLabel.Size = new Size(91, 19);
            lblNomePraticaLabel.TabIndex = 1;
            lblNomePraticaLabel.Text = "Nome pratica";
            // 
            // lblPraticaHeader
            // 
            lblPraticaHeader.AutoSize = true;
            lblPraticaHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblPraticaHeader.ForeColor = Color.FromArgb(0, 95, 184);
            lblPraticaHeader.Location = new Point(17, 14);
            lblPraticaHeader.Name = "lblPraticaHeader";
            lblPraticaHeader.Size = new Size(63, 21);
            lblPraticaHeader.TabIndex = 0;
            lblPraticaHeader.Text = "Pratica";
            // 
            // cardCondivisioniMain
            // 
            cardCondivisioniMain.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardCondivisioniMain.BackColor = Color.FromArgb(255, 255, 255);
            cardCondivisioniMain.Controls.Add(btnRevocaSelezionati);
            cardCondivisioniMain.Controls.Add(btnCondividiSelezionati);
            cardCondivisioniMain.Controls.Add(cardDettagliCartella);
            cardCondivisioniMain.Controls.Add(cardEsterni);
            cardCondivisioniMain.Controls.Add(cardInterni);
            cardCondivisioniMain.Controls.Add(lblCondivisioniHeader);
            cardCondivisioniMain.Depth = 0;
            cardCondivisioniMain.ForeColor = Color.FromArgb(222, 0, 0, 0);
            cardCondivisioniMain.Location = new Point(14, 254);
            cardCondivisioniMain.Margin = new Padding(14);
            cardCondivisioniMain.MouseState = MaterialSkin.MouseState.HOVER;
            cardCondivisioniMain.Name = "cardCondivisioniMain";
            cardCondivisioniMain.Padding = new Padding(14);
            cardCondivisioniMain.Size = new Size(1458, 420);
            cardCondivisioniMain.TabIndex = 3;
            // 
            // btnRevocaSelezionati
            // 
            btnRevocaSelezionati.AutoSize = false;
            btnRevocaSelezionati.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRevocaSelezionati.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnRevocaSelezionati.Depth = 0;
            btnRevocaSelezionati.HighEmphasis = false;
            btnRevocaSelezionati.Icon = null;
            btnRevocaSelezionati.Location = new Point(710, 360);
            btnRevocaSelezionati.Margin = new Padding(4, 6, 4, 6);
            btnRevocaSelezionati.MouseState = MaterialSkin.MouseState.HOVER;
            btnRevocaSelezionati.Name = "btnRevocaSelezionati";
            btnRevocaSelezionati.NoAccentTextColor = Color.Empty;
            btnRevocaSelezionati.Size = new Size(220, 45);
            btnRevocaSelezionati.TabIndex = 5;
            btnRevocaSelezionati.Text = "Revoca selezionati";
            btnRevocaSelezionati.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Outlined;
            btnRevocaSelezionati.UseAccentColor = false;
            // 
            // btnCondividiSelezionati
            // 
            btnCondividiSelezionati.AutoSize = false;
            btnCondividiSelezionati.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCondividiSelezionati.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnCondividiSelezionati.Depth = 0;
            btnCondividiSelezionati.HighEmphasis = true;
            btnCondividiSelezionati.Icon = null;
            btnCondividiSelezionati.Location = new Point(470, 360);
            btnCondividiSelezionati.Margin = new Padding(4, 6, 4, 6);
            btnCondividiSelezionati.MouseState = MaterialSkin.MouseState.HOVER;
            btnCondividiSelezionati.Name = "btnCondividiSelezionati";
            btnCondividiSelezionati.NoAccentTextColor = Color.Empty;
            btnCondividiSelezionati.Size = new Size(220, 45);
            btnCondividiSelezionati.TabIndex = 4;
            btnCondividiSelezionati.Text = "Condividi selezionati";
            btnCondividiSelezionati.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            btnCondividiSelezionati.UseAccentColor = false;
            // 
            // cardDettagliCartella
            // 
            cardDettagliCartella.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cardDettagliCartella.BorderStyle = BorderStyle.FixedSingle;
            cardDettagliCartella.Controls.Add(lblDettaglioCreatore);
            cardDettagliCartella.Controls.Add(lblDettaglioData);
            cardDettagliCartella.Controls.Add(lblDettaglioID);
            cardDettagliCartella.Controls.Add(lblDettaglioPercorso);
            cardDettagliCartella.Controls.Add(lblCreataDa);
            cardDettagliCartella.Controls.Add(lblCreataIl);
            cardDettagliCartella.Controls.Add(lblIdCartella);
            cardDettagliCartella.Controls.Add(lblPercorso);
            cardDettagliCartella.Controls.Add(lblNoCartella);
            cardDettagliCartella.Controls.Add(lblCartellaSelezionataHeader);
            cardDettagliCartella.Location = new Point(1174, 45);
            cardDettagliCartella.Name = "cardDettagliCartella";
            cardDettagliCartella.Size = new Size(268, 298);
            cardDettagliCartella.TabIndex = 3;
            // 
            // lblDettaglioCreatore
            // 
            lblDettaglioCreatore.AutoSize = true;
            lblDettaglioCreatore.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblDettaglioCreatore.Location = new Point(15, 255);
            lblDettaglioCreatore.Name = "lblDettaglioCreatore";
            lblDettaglioCreatore.Size = new Size(12, 15);
            lblDettaglioCreatore.TabIndex = 9;
            lblDettaglioCreatore.Text = "-";
            // 
            // lblDettaglioData
            // 
            lblDettaglioData.AutoSize = true;
            lblDettaglioData.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblDettaglioData.Location = new Point(15, 200);
            lblDettaglioData.Name = "lblDettaglioData";
            lblDettaglioData.Size = new Size(12, 15);
            lblDettaglioData.TabIndex = 8;
            lblDettaglioData.Text = "-";
            // 
            // lblDettaglioID
            // 
            lblDettaglioID.AutoSize = true;
            lblDettaglioID.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblDettaglioID.Location = new Point(15, 145);
            lblDettaglioID.Name = "lblDettaglioID";
            lblDettaglioID.Size = new Size(12, 15);
            lblDettaglioID.TabIndex = 7;
            lblDettaglioID.Text = "-";
            // 
            // lblDettaglioPercorso
            // 
            lblDettaglioPercorso.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            lblDettaglioPercorso.Location = new Point(15, 90);
            lblDettaglioPercorso.Name = "lblDettaglioPercorso";
            lblDettaglioPercorso.Size = new Size(235, 35);
            lblDettaglioPercorso.TabIndex = 6;
            lblDettaglioPercorso.Text = "-";
            // 
            // lblCreataDa
            // 
            lblCreataDa.AutoSize = true;
            lblCreataDa.Font = new Font("Segoe UI", 8.25F);
            lblCreataDa.ForeColor = Color.Gray;
            lblCreataDa.Location = new Point(15, 237);
            lblCreataDa.Name = "lblCreataDa";
            lblCreataDa.Size = new Size(56, 13);
            lblCreataDa.TabIndex = 5;
            lblCreataDa.Text = "Creata da";
            // 
            // lblCreataIl
            // 
            lblCreataIl.AutoSize = true;
            lblCreataIl.Font = new Font("Segoe UI", 8.25F);
            lblCreataIl.ForeColor = Color.Gray;
            lblCreataIl.Location = new Point(15, 182);
            lblCreataIl.Name = "lblCreataIl";
            lblCreataIl.Size = new Size(49, 13);
            lblCreataIl.TabIndex = 4;
            lblCreataIl.Text = "Creata il";
            // 
            // lblIdCartella
            // 
            lblIdCartella.AutoSize = true;
            lblIdCartella.Font = new Font("Segoe UI", 8.25F);
            lblIdCartella.ForeColor = Color.Gray;
            lblIdCartella.Location = new Point(15, 127);
            lblIdCartella.Name = "lblIdCartella";
            lblIdCartella.Size = new Size(58, 13);
            lblIdCartella.TabIndex = 3;
            lblIdCartella.Text = "ID cartella";
            // 
            // lblPercorso
            // 
            lblPercorso.AutoSize = true;
            lblPercorso.Font = new Font("Segoe UI", 8.25F);
            lblPercorso.ForeColor = Color.Gray;
            lblPercorso.Location = new Point(15, 72);
            lblPercorso.Name = "lblPercorso";
            lblPercorso.Size = new Size(51, 13);
            lblPercorso.TabIndex = 2;
            lblPercorso.Text = "Percorso";
            // 
            // lblNoCartella
            // 
            lblNoCartella.AutoSize = true;
            lblNoCartella.Font = new Font("Microsoft Sans Serif", 9F);
            lblNoCartella.ForeColor = Color.Gray;
            lblNoCartella.Location = new Point(15, 45);
            lblNoCartella.Name = "lblNoCartella";
            lblNoCartella.Size = new Size(165, 15);
            lblNoCartella.TabIndex = 1;
            lblNoCartella.Text = "Nessuna cartella selezionata";
            // 
            // lblCartellaSelezionataHeader
            // 
            lblCartellaSelezionataHeader.AutoSize = true;
            lblCartellaSelezionataHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblCartellaSelezionataHeader.Location = new Point(14, 15);
            lblCartellaSelezionataHeader.Name = "lblCartellaSelezionataHeader";
            lblCartellaSelezionataHeader.Size = new Size(140, 19);
            lblCartellaSelezionataHeader.TabIndex = 0;
            lblCartellaSelezionataHeader.Text = "Cartella selezionata";
            // 
            // cardEsterni
            // 
            cardEsterni.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardEsterni.BackColor = Color.FromArgb(255, 255, 255);
            cardEsterni.Controls.Add(btnRevocaEsterno);
            cardEsterni.Controls.Add(btnModificaRuoloEsterno);
            cardEsterni.Controls.Add(btnAggiungiEsterno);
            cardEsterni.Controls.Add(lblNoEsterni);
            cardEsterni.Controls.Add(listViewEsterni);
            cardEsterni.Controls.Add(lblEsterniHeader);
            cardEsterni.Depth = 0;
            cardEsterni.ForeColor = Color.FromArgb(222, 0, 0, 0);
            cardEsterni.Location = new Point(590, 45);
            cardEsterni.Margin = new Padding(14);
            cardEsterni.MouseState = MaterialSkin.MouseState.HOVER;
            cardEsterni.Name = "cardEsterni";
            cardEsterni.Padding = new Padding(14);
            cardEsterni.Size = new Size(568, 298);
            cardEsterni.TabIndex = 2;
            // 
            // btnRevocaEsterno
            // 
            btnRevocaEsterno.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRevocaEsterno.AutoSize = false;
            btnRevocaEsterno.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRevocaEsterno.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnRevocaEsterno.Depth = 0;
            btnRevocaEsterno.HighEmphasis = false;
            btnRevocaEsterno.Icon = null;
            btnRevocaEsterno.Location = new Point(428, 248);
            btnRevocaEsterno.Margin = new Padding(4, 6, 4, 6);
            btnRevocaEsterno.MouseState = MaterialSkin.MouseState.HOVER;
            btnRevocaEsterno.Name = "btnRevocaEsterno";
            btnRevocaEsterno.NoAccentTextColor = Color.Empty;
            btnRevocaEsterno.Size = new Size(120, 36);
            btnRevocaEsterno.TabIndex = 5;
            btnRevocaEsterno.Text = "Revoca";
            btnRevocaEsterno.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Text;
            btnRevocaEsterno.UseAccentColor = false;
            // 
            // btnModificaRuoloEsterno
            // 
            btnModificaRuoloEsterno.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnModificaRuoloEsterno.AutoSize = false;
            btnModificaRuoloEsterno.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnModificaRuoloEsterno.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnModificaRuoloEsterno.Depth = 0;
            btnModificaRuoloEsterno.HighEmphasis = false;
            btnModificaRuoloEsterno.Icon = null;
            btnModificaRuoloEsterno.Location = new Point(288, 248);
            btnModificaRuoloEsterno.Margin = new Padding(4, 6, 4, 6);
            btnModificaRuoloEsterno.MouseState = MaterialSkin.MouseState.HOVER;
            btnModificaRuoloEsterno.Name = "btnModificaRuoloEsterno";
            btnModificaRuoloEsterno.NoAccentTextColor = Color.Empty;
            btnModificaRuoloEsterno.Size = new Size(120, 36);
            btnModificaRuoloEsterno.TabIndex = 4;
            btnModificaRuoloEsterno.Text = "Modifica ruolo";
            btnModificaRuoloEsterno.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Text;
            btnModificaRuoloEsterno.UseAccentColor = false;
            // 
            // btnAggiungiEsterno
            // 
            btnAggiungiEsterno.AutoSize = false;
            btnAggiungiEsterno.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnAggiungiEsterno.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnAggiungiEsterno.Depth = 0;
            btnAggiungiEsterno.HighEmphasis = true;
            btnAggiungiEsterno.Icon = null;
            btnAggiungiEsterno.Location = new Point(17, 248);
            btnAggiungiEsterno.Margin = new Padding(4, 6, 4, 6);
            btnAggiungiEsterno.MouseState = MaterialSkin.MouseState.HOVER;
            btnAggiungiEsterno.Name = "btnAggiungiEsterno";
            btnAggiungiEsterno.NoAccentTextColor = Color.Empty;
            btnAggiungiEsterno.Size = new Size(135, 36);
            btnAggiungiEsterno.TabIndex = 3;
            btnAggiungiEsterno.Text = "+ Aggiungi esterno";
            btnAggiungiEsterno.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Outlined;
            btnAggiungiEsterno.UseAccentColor = false;
            // 
            // lblNoEsterni
            // 
            lblNoEsterni.AutoSize = true;
            lblNoEsterni.BackColor = Color.White;
            lblNoEsterni.Font = new Font("Segoe UI Light", 10F);
            lblNoEsterni.ForeColor = Color.Gray;
            lblNoEsterni.Location = new Point(135, 127);
            lblNoEsterni.Name = "lblNoEsterni";
            lblNoEsterni.Size = new Size(178, 19);
            lblNoEsterni.TabIndex = 2;
            lblNoEsterni.Text = "Nessuna condivisione esterna";
            // 
            // listViewEsterni
            // 
            listViewEsterni.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewEsterni.AutoSizeTable = false;
            listViewEsterni.BackColor = Color.FromArgb(255, 255, 255);
            listViewEsterni.BorderStyle = BorderStyle.None;
            listViewEsterni.Depth = 0;
            listViewEsterni.FullRowSelect = true;
            listViewEsterni.Location = new Point(17, 45);
            listViewEsterni.MinimumSize = new Size(200, 100);
            listViewEsterni.MouseLocation = new Point(-1, -1);
            listViewEsterni.MouseState = MaterialSkin.MouseState.HOVER;
            listViewEsterni.Name = "listViewEsterni";
            listViewEsterni.OwnerDraw = true;
            listViewEsterni.Size = new Size(534, 185);
            listViewEsterni.TabIndex = 1;
            listViewEsterni.UseCompatibleStateImageBehavior = false;
            listViewEsterni.View = View.Details;
            // 
            // lblEsterniHeader
            // 
            lblEsterniHeader.AutoSize = true;
            lblEsterniHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblEsterniHeader.ForeColor = Color.FromArgb(60, 60, 60);
            lblEsterniHeader.Location = new Point(14, 14);
            lblEsterniHeader.Name = "lblEsterniHeader";
            lblEsterniHeader.Size = new Size(198, 17);
            lblEsterniHeader.TabIndex = 0;
            lblEsterniHeader.Text = "Esterni (ospiti o utenti esterni)";
            // 
            // cardInterni
            // 
            cardInterni.BackColor = Color.FromArgb(255, 255, 255);
            cardInterni.Controls.Add(btnRimuoviInterno);
            cardInterni.Controls.Add(btnModificaRuoloInterno);
            cardInterni.Controls.Add(btnAggiungiInterno);
            cardInterni.Controls.Add(lblNoInterni);
            cardInterni.Controls.Add(listViewInterni);
            cardInterni.Controls.Add(lblInterniHeader);
            cardInterni.Depth = 0;
            cardInterni.ForeColor = Color.FromArgb(222, 0, 0, 0);
            cardInterni.Location = new Point(17, 45);
            cardInterni.Margin = new Padding(14);
            cardInterni.MouseState = MaterialSkin.MouseState.HOVER;
            cardInterni.Name = "cardInterni";
            cardInterni.Padding = new Padding(14);
            cardInterni.Size = new Size(550, 298);
            cardInterni.TabIndex = 1;
            // 
            // btnRimuoviInterno
            // 
            btnRimuoviInterno.AutoSize = false;
            btnRimuoviInterno.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnRimuoviInterno.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnRimuoviInterno.Depth = 0;
            btnRimuoviInterno.HighEmphasis = false;
            btnRimuoviInterno.Icon = null;
            btnRimuoviInterno.Location = new Point(410, 248);
            btnRimuoviInterno.Margin = new Padding(4, 6, 4, 6);
            btnRimuoviInterno.MouseState = MaterialSkin.MouseState.HOVER;
            btnRimuoviInterno.Name = "btnRimuoviInterno";
            btnRimuoviInterno.NoAccentTextColor = Color.Empty;
            btnRimuoviInterno.Size = new Size(120, 36);
            btnRimuoviInterno.TabIndex = 5;
            btnRimuoviInterno.Text = "Rimuovi";
            btnRimuoviInterno.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Text;
            btnRimuoviInterno.UseAccentColor = false;
            // 
            // btnModificaRuoloInterno
            // 
            btnModificaRuoloInterno.AutoSize = false;
            btnModificaRuoloInterno.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnModificaRuoloInterno.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnModificaRuoloInterno.Depth = 0;
            btnModificaRuoloInterno.HighEmphasis = false;
            btnModificaRuoloInterno.Icon = null;
            btnModificaRuoloInterno.Location = new Point(270, 248);
            btnModificaRuoloInterno.Margin = new Padding(4, 6, 4, 6);
            btnModificaRuoloInterno.MouseState = MaterialSkin.MouseState.HOVER;
            btnModificaRuoloInterno.Name = "btnModificaRuoloInterno";
            btnModificaRuoloInterno.NoAccentTextColor = Color.Empty;
            btnModificaRuoloInterno.Size = new Size(120, 36);
            btnModificaRuoloInterno.TabIndex = 4;
            btnModificaRuoloInterno.Text = "Modifica ruolo";
            btnModificaRuoloInterno.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Text;
            btnModificaRuoloInterno.UseAccentColor = false;
            // 
            // btnAggiungiInterno
            // 
            btnAggiungiInterno.AutoSize = false;
            btnAggiungiInterno.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnAggiungiInterno.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnAggiungiInterno.Depth = 0;
            btnAggiungiInterno.HighEmphasis = true;
            btnAggiungiInterno.Icon = null;
            btnAggiungiInterno.Location = new Point(17, 248);
            btnAggiungiInterno.Margin = new Padding(4, 6, 4, 6);
            btnAggiungiInterno.MouseState = MaterialSkin.MouseState.HOVER;
            btnAggiungiInterno.Name = "btnAggiungiInterno";
            btnAggiungiInterno.NoAccentTextColor = Color.Empty;
            btnAggiungiInterno.Size = new Size(135, 36);
            btnAggiungiInterno.TabIndex = 3;
            btnAggiungiInterno.Text = "+ Aggiungi interno";
            btnAggiungiInterno.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Outlined;
            btnAggiungiInterno.UseAccentColor = false;
            // 
            // lblNoInterni
            // 
            lblNoInterni.AutoSize = true;
            lblNoInterni.BackColor = Color.White;
            lblNoInterni.Font = new Font("Segoe UI Light", 10F);
            lblNoInterni.ForeColor = Color.Gray;
            lblNoInterni.Location = new Point(135, 127);
            lblNoInterni.Name = "lblNoInterni";
            lblNoInterni.Size = new Size(176, 19);
            lblNoInterni.TabIndex = 2;
            lblNoInterni.Text = "Nessuna condivisione interna";
            // 
            // listViewInterni
            // 
            listViewInterni.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewInterni.AutoSizeTable = false;
            listViewInterni.BackColor = Color.FromArgb(255, 255, 255);
            listViewInterni.BorderStyle = BorderStyle.None;
            listViewInterni.Depth = 0;
            listViewInterni.FullRowSelect = true;
            listViewInterni.Location = new Point(17, 45);
            listViewInterni.MinimumSize = new Size(200, 100);
            listViewInterni.MouseLocation = new Point(-1, -1);
            listViewInterni.MouseState = MaterialSkin.MouseState.HOVER;
            listViewInterni.Name = "listViewInterni";
            listViewInterni.OwnerDraw = true;
            listViewInterni.Size = new Size(516, 185);
            listViewInterni.TabIndex = 1;
            listViewInterni.UseCompatibleStateImageBehavior = false;
            listViewInterni.View = View.Details;
            // 
            // lblInterniHeader
            // 
            lblInterniHeader.AutoSize = true;
            lblInterniHeader.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblInterniHeader.ForeColor = Color.FromArgb(60, 60, 60);
            lblInterniHeader.Location = new Point(14, 14);
            lblInterniHeader.Name = "lblInterniHeader";
            lblInterniHeader.Size = new Size(170, 17);
            lblInterniHeader.TabIndex = 0;
            lblInterniHeader.Text = "Interni (utente del tenant)";
            // 
            // lblCondivisioniHeader
            // 
            lblCondivisioniHeader.AutoSize = true;
            lblCondivisioniHeader.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblCondivisioniHeader.ForeColor = Color.FromArgb(0, 95, 184);
            lblCondivisioniHeader.Location = new Point(17, 14);
            lblCondivisioniHeader.Name = "lblCondivisioniHeader";
            lblCondivisioniHeader.Size = new Size(106, 21);
            lblCondivisioniHeader.TabIndex = 0;
            lblCondivisioniHeader.Text = "Condivisioni";
            // 
            // cardLog
            // 
            cardLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cardLog.BackColor = Color.FromArgb(255, 255, 255);
            cardLog.Controls.Add(btnPulisciLog);
            cardLog.Controls.Add(listViewLog);
            cardLog.Controls.Add(lblLogHeader);
            cardLog.Depth = 0;
            cardLog.ForeColor = Color.FromArgb(222, 0, 0, 0);
            cardLog.Location = new Point(14, 684);
            cardLog.Margin = new Padding(14);
            cardLog.MouseState = MaterialSkin.MouseState.HOVER;
            cardLog.Name = "cardLog";
            cardLog.Padding = new Padding(14);
            cardLog.Size = new Size(1458, 162);
            cardLog.TabIndex = 4;
            // 
            // btnPulisciLog
            // 
            btnPulisciLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPulisciLog.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnPulisciLog.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnPulisciLog.Depth = 0;
            btnPulisciLog.HighEmphasis = false;
            btnPulisciLog.Icon = null;
            btnPulisciLog.Location = new Point(1327, 12);
            btnPulisciLog.Margin = new Padding(4, 6, 4, 6);
            btnPulisciLog.MouseState = MaterialSkin.MouseState.HOVER;
            btnPulisciLog.Name = "btnPulisciLog";
            btnPulisciLog.NoAccentTextColor = Color.Empty;
            btnPulisciLog.Size = new Size(107, 36);
            btnPulisciLog.TabIndex = 2;
            btnPulisciLog.Text = "Pulisci log";
            btnPulisciLog.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Outlined;
            btnPulisciLog.UseAccentColor = false;
            btnPulisciLog.Click += btnPulisciLog_Click;
            // 
            // listViewLog
            // 
            listViewLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            listViewLog.AutoSizeTable = false;
            listViewLog.BackColor = Color.FromArgb(255, 255, 255);
            listViewLog.BorderStyle = BorderStyle.None;
            listViewLog.Depth = 0;
            listViewLog.FullRowSelect = true;
            listViewLog.Location = new Point(17, 47);
            listViewLog.MinimumSize = new Size(200, 50);
            listViewLog.MouseLocation = new Point(-1, -1);
            listViewLog.MouseState = MaterialSkin.MouseState.HOVER;
            listViewLog.Name = "listViewLog";
            listViewLog.OwnerDraw = true;
            listViewLog.Size = new Size(1424, 98);
            listViewLog.TabIndex = 1;
            listViewLog.UseCompatibleStateImageBehavior = false;
            listViewLog.View = View.Details;
            // 
            // lblLogHeader
            // 
            lblLogHeader.AutoSize = true;
            lblLogHeader.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblLogHeader.ForeColor = Color.FromArgb(40, 40, 40);
            lblLogHeader.Location = new Point(17, 14);
            lblLogHeader.Name = "lblLogHeader";
            lblLogHeader.Size = new Size(35, 20);
            lblLogHeader.TabIndex = 0;
            lblLogHeader.Text = "Log";
            // 
            // tabPratiche
            // 
            tabPratiche.Location = new Point(4, 24);
            tabPratiche.Name = "tabPratiche";
            tabPratiche.Size = new Size(1486, 854);
            tabPratiche.TabIndex = 1;
            tabPratiche.Text = "Pratiche";
            // 
            // tabCondivisioni
            // 
            tabCondivisioni.Location = new Point(4, 24);
            tabCondivisioni.Name = "tabCondivisioni";
            tabCondivisioni.Size = new Size(1486, 854);
            tabCondivisioni.TabIndex = 2;
            tabCondivisioni.Text = "Condivisioni";
            // 
            // tabRicerca
            // 
            tabRicerca.Location = new Point(4, 24);
            tabRicerca.Name = "tabRicerca";
            tabRicerca.Size = new Size(1486, 854);
            tabRicerca.TabIndex = 3;
            tabRicerca.Text = "Ricerca";
            // 
            // tabPermessi
            // 
            tabPermessi.Location = new Point(4, 24);
            tabPermessi.Name = "tabPermessi";
            tabPermessi.Size = new Size(1486, 854);
            tabPermessi.TabIndex = 4;
            tabPermessi.Text = "Permessi";
            // 
            // tabAttivita
            // 
            tabAttivita.Location = new Point(4, 24);
            tabAttivita.Name = "tabAttivita";
            tabAttivita.Size = new Size(1486, 854);
            tabAttivita.TabIndex = 5;
            tabAttivita.Text = "Attività recenti";
            // 
            // tabImpostazioni
            // 
            tabImpostazioni.Location = new Point(4, 24);
            tabImpostazioni.Name = "tabImpostazioni";
            tabImpostazioni.Size = new Size(1486, 854);
            tabImpostazioni.TabIndex = 6;
            tabImpostazioni.Text = "Impostazioni";
            // 
            // panelStatus
            // 
            panelStatus.BackColor = Color.FromArgb(240, 244, 248);
            panelStatus.Controls.Add(lblStatusAggiornamento);
            panelStatus.Controls.Add(lblStatusVersione);
            panelStatus.Controls.Add(lblStatusSito);
            panelStatus.Controls.Add(lblStatusTenant);
            panelStatus.Controls.Add(lblStatusUtente);
            panelStatus.Controls.Add(lblStatusConnesso);
            panelStatus.Dock = DockStyle.Bottom;
            panelStatus.Location = new Point(3, 911);
            panelStatus.Name = "panelStatus";
            panelStatus.Size = new Size(1494, 35);
            panelStatus.TabIndex = 1;
            // 
            // lblStatusAggiornamento
            // 
            lblStatusAggiornamento.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblStatusAggiornamento.AutoSize = true;
            lblStatusAggiornamento.Font = new Font("Segoe UI", 8.25F);
            lblStatusAggiornamento.ForeColor = Color.FromArgb(60, 60, 60);
            lblStatusAggiornamento.Location = new Point(1150, 11);
            lblStatusAggiornamento.Name = "lblStatusAggiornamento";
            lblStatusAggiornamento.Size = new Size(127, 13);
            lblStatusAggiornamento.TabIndex = 0;
            lblStatusAggiornamento.Text = "Ultimo aggiornamento:";
            // 
            // lblStatusVersione
            // 
            lblStatusVersione.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblStatusVersione.AutoSize = true;
            lblStatusVersione.Font = new Font("Segoe UI", 8.25F);
            lblStatusVersione.ForeColor = Color.FromArgb(60, 60, 60);
            lblStatusVersione.Location = new Point(1220, 11);
            lblStatusVersione.Name = "lblStatusVersione";
            lblStatusVersione.Size = new Size(81, 13);
            lblStatusVersione.TabIndex = 4;
            lblStatusVersione.Text = "Versione: 1.0.0";
            // 
            // lblStatusSito
            // 
            lblStatusSito.AutoSize = true;
            lblStatusSito.Font = new Font("Segoe UI", 8.25F);
            lblStatusSito.ForeColor = Color.FromArgb(60, 60, 60);
            lblStatusSito.Location = new Point(750, 11);
            lblStatusSito.Name = "lblStatusSito";
            lblStatusSito.Size = new Size(30, 13);
            lblStatusSito.TabIndex = 3;
            lblStatusSito.Text = "Sito:";
            // 
            // lblStatusTenant
            // 
            lblStatusTenant.AutoSize = true;
            lblStatusTenant.Font = new Font("Segoe UI", 8.25F);
            lblStatusTenant.ForeColor = Color.FromArgb(60, 60, 60);
            lblStatusTenant.Location = new Point(400, 11);
            lblStatusTenant.Name = "lblStatusTenant";
            lblStatusTenant.Size = new Size(44, 13);
            lblStatusTenant.TabIndex = 2;
            lblStatusTenant.Text = "Tenant:";
            // 
            // lblStatusUtente
            // 
            lblStatusUtente.AutoSize = true;
            lblStatusUtente.Font = new Font("Segoe UI", 8.25F);
            lblStatusUtente.ForeColor = Color.FromArgb(60, 60, 60);
            lblStatusUtente.Location = new Point(150, 11);
            lblStatusUtente.Name = "lblStatusUtente";
            lblStatusUtente.Size = new Size(45, 13);
            lblStatusUtente.TabIndex = 1;
            lblStatusUtente.Text = "Utente:";
            // 
            // lblStatusConnesso
            // 
            lblStatusConnesso.AutoSize = true;
            lblStatusConnesso.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            lblStatusConnesso.ForeColor = Color.Green;
            lblStatusConnesso.Location = new Point(14, 11);
            lblStatusConnesso.Name = "lblStatusConnesso";
            lblStatusConnesso.Size = new Size(58, 13);
            lblStatusConnesso.TabIndex = 0;
            lblStatusConnesso.Text = "Connesso";
            // 
            // MainMaterialForm
            // 
            ClientSize = new Size(1500, 949);
            Controls.Add(panelStatus);
            Controls.Add(materialTabControl1);
            DrawerTabControl = materialTabControl1;
            Name = "MainMaterialForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SharePoint Toolbox";
            Load += MainMaterialForm_Load;
            materialTabControl1.ResumeLayout(false);
            tabDashboard.ResumeLayout(false);
            cardUtente.ResumeLayout(false);
            cardUtente.PerformLayout();
            cardSharePoint.ResumeLayout(false);
            cardSharePoint.PerformLayout();
            cardPratica.ResumeLayout(false);
            cardPratica.PerformLayout();
            cardCondivisioniMain.ResumeLayout(false);
            cardCondivisioniMain.PerformLayout();
            cardDettagliCartella.ResumeLayout(false);
            cardDettagliCartella.PerformLayout();
            cardEsterni.ResumeLayout(false);
            cardEsterni.PerformLayout();
            cardInterni.ResumeLayout(false);
            cardInterni.PerformLayout();
            cardLog.ResumeLayout(false);
            cardLog.PerformLayout();
            panelStatus.ResumeLayout(false);
            panelStatus.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private MaterialSkin.Controls.MaterialTabControl materialTabControl1;
        private System.Windows.Forms.TabPage tabDashboard;
        private System.Windows.Forms.TabPage tabPratiche;
        private System.Windows.Forms.TabPage tabCondivisioni;
        private System.Windows.Forms.TabPage tabRicerca;
        private System.Windows.Forms.TabPage tabPermessi;
        private System.Windows.Forms.TabPage tabAttivita;
        private System.Windows.Forms.TabPage tabImpostazioni;
        private MaterialSkin.Controls.MaterialCard cardUtente;
        private System.Windows.Forms.Label lblUtenteEmail;
        private System.Windows.Forms.Label lblUtenteNome;
        private System.Windows.Forms.Label lblUtenteHeader;
        private System.Windows.Forms.Panel panelAvatarUtente;
        private MaterialSkin.Controls.MaterialCard cardSharePoint;
        private MaterialSkin.Controls.MaterialButton btnRiconnetti;
        private System.Windows.Forms.Label lblSitoUrl;
        private System.Windows.Forms.Label lblSitoNome;
        private System.Windows.Forms.Label lblSharePointHeader;
        private System.Windows.Forms.Panel panelIconaSharePoint;
        private MaterialSkin.Controls.MaterialCard cardPratica;
        private MaterialSkin.Controls.MaterialButton btnCreaCartella;
        private MaterialSkin.Controls.MaterialTextBox2 txtNomePratica;
        private System.Windows.Forms.Label lblNomePraticaLabel;
        private System.Windows.Forms.Label lblPraticaHeader;
        private MaterialSkin.Controls.MaterialCard cardCondivisioniMain;
        private System.Windows.Forms.Label lblCondivisioniHeader;
        private MaterialSkin.Controls.MaterialCard cardInterni;
        private System.Windows.Forms.Label lblInterniHeader;
        private MaterialSkin.Controls.MaterialListView listViewInterni;
        private System.Windows.Forms.Label lblNoInterni;
        private MaterialSkin.Controls.MaterialButton btnAggiungiInterno;
        private MaterialSkin.Controls.MaterialButton btnRimuoviInterno;
        private MaterialSkin.Controls.MaterialButton btnModificaRuoloInterno;
        private MaterialSkin.Controls.MaterialCard cardEsterni;
        private MaterialSkin.Controls.MaterialButton btnRevocaEsterno;
        private MaterialSkin.Controls.MaterialButton btnModificaRuoloEsterno;
        private MaterialSkin.Controls.MaterialButton btnAggiungiEsterno;
        private System.Windows.Forms.Label lblNoEsterni;
        private MaterialSkin.Controls.MaterialListView listViewEsterni;
        private System.Windows.Forms.Label lblEsterniHeader;
        private System.Windows.Forms.Panel cardDettagliCartella;
        private System.Windows.Forms.Label lblCartellaSelezionataHeader;
        private System.Windows.Forms.Label lblNoCartella;
        private System.Windows.Forms.Label lblPercorso;
        private System.Windows.Forms.Label lblCreataDa;
        private System.Windows.Forms.Label lblCreataIl;
        private System.Windows.Forms.Label lblIdCartella;
        private System.Windows.Forms.Label lblDettaglioCreatore;
        private System.Windows.Forms.Label lblDettaglioData;
        private System.Windows.Forms.Label lblDettaglioID;
        private System.Windows.Forms.Label lblDettaglioPercorso;
        private MaterialSkin.Controls.MaterialButton btnRevocaSelezionati;
        private MaterialSkin.Controls.MaterialButton btnCondividiSelezionati;
        private MaterialSkin.Controls.MaterialCard cardLog;
        private MaterialSkin.Controls.MaterialButton btnPulisciLog;
        private MaterialSkin.Controls.MaterialListView listViewLog;
        private System.Windows.Forms.Label lblLogHeader;
        private System.Windows.Forms.Panel panelStatus;
        private System.Windows.Forms.Label lblStatusConnesso;
        private System.Windows.Forms.Label lblStatusAggiornamento;
        private System.Windows.Forms.Label lblStatusVersione;
        private System.Windows.Forms.Label lblStatusSito;
        private System.Windows.Forms.Label lblStatusTenant;
        private System.Windows.Forms.Label lblStatusUtente;
    }
}