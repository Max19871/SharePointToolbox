using SharePointToolbox.Microsoft365;
using SharePointToolbox.Models;
using SharePointToolbox.Helpers;

namespace SharePointToolbox.UI;

public partial class MainForm : Form
{
    // ===========================
    // Campi privati
    // ===========================

    // Servizio che gestisce l'autenticazione Microsoft
    private readonly AuthenticationService _authenticationService;

    // Servizio SharePoint
    private readonly SharePointService _sharePointService;

    // ===========================
    // Costruttore
    // ===========================

    public MainForm(
    AuthenticationService authenticationService,
    SharePointService sharePointService)
    {
        InitializeComponent();
        UiLogger.Initialize(AddLogLine);
        btnCreateFolder.Enabled = false;
        _authenticationService = authenticationService;
        _sharePointService = sharePointService;
    }

    // ===========================
    // Event handlers
    // ===========================

    private async void btnLogin_Click(object sender, EventArgs e)
    {
        try
        {
            var result = await _authenticationService.SignInAsync();

            // Visualizza l'utente autenticato
            lblUser.Text = result.Account.Username;
            UiLogger.Info($"Login effettuato: {result.Account.Username}");

            // Recupera la Document Library
            DocumentLibrary library =
                await _sharePointService.GetDefaultDocumentLibraryAsync();

            // Visualizza il nome della libreria
            lblSite.Text = library.Name;
            UiLogger.Info($"Libreria trovata: {library.Name}");

            // Abilita il pulsante
            btnCreateFolder.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async void btnCreateFolder_Click(object sender, EventArgs e) 
    {
        string folderName = txtPracticeName.Text.Trim();

        if (string.IsNullOrWhiteSpace(folderName))
        {
            MessageBox.Show(
                "Inserisci il nome della pratica.",
                "Attenzione",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        await _sharePointService.CreateFolderAsync(folderName);
        UiLogger.Info($"Cartella creata: {folderName}");

        /*
            MessageBox.Show(
            $"Cartella '{folderName}' creata con successo.",
            "SharePoint",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information); */
    }

    /// <summary>
    /// Aggiunge una riga al log.
    /// </summary>
    private void AddLogLine(string text)
    {
        txtLog.AppendText(text + Environment.NewLine);
    }
}