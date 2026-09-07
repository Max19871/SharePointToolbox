using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using SharePointToolbox.Configuration;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.UI.Theming;

namespace SharePointToolbox.UI;

public sealed class StartupAuthorizationForm : ShapedStartupSplashForm
{
    private readonly AuthenticationService _authenticationService;
    private readonly ApplicationAuthorizationSession _session;
    private readonly SecurityOptions _security;
    private readonly PressReviewService _pressReviewService;
    private readonly IServiceProvider _services;
    private bool _started;
    public Form? ReadyForm { get; private set; }

    public StartupAuthorizationForm(
        AuthenticationService authenticationService,
        ApplicationAuthorizationSession session,
        IOptions<SecurityOptions> security,
        PressReviewService pressReviewService,
        AppThemeManager themeManager,
        IServiceProvider services)
        : base(showInTaskbar: true)
    {
        _authenticationService = authenticationService;
        _session = session;
        _security = security.Value;
        _pressReviewService = pressReviewService;
        _services = services;
        Shown += async (_, _) => await VerifyAccessAsync();
    }

    private async Task VerifyAccessAsync()
    {
        if (_started) return;
        _started = true;
        try
        {
            ReportAuthorization(5, "Verifica della sessione Microsoft 365…");
            await _authenticationService.SignInAsync();
            ReportAuthorization(35, "Identificazione dell'utente…");
            AuthenticationService.CurrentUserProfile user =
                await _authenticationService.GetCurrentUserProfileAsync();

            if (string.IsNullOrWhiteSpace(_security.ApplicationUsersGroupId))
            {
                ShowDenied(user.Email, configurationMissing: true);
                return;
            }

            ReportAuthorization(70, "Verifica dei privilegi di utilizzo…");
            bool authorized = await _authenticationService.IsCurrentUserMemberOfGroupAsync(
                _security.ApplicationUsersGroupId);
            if (!authorized)
            {
                ShowDenied(user.Email, configurationMissing: false);
                return;
            }

            _session.Authorize(user);
            ReportAuthorization(100, "Accesso autorizzato.");
            ReportInitialization(4, "Preparazione della Data Room…");
            MainForm dataRoom = _services.GetRequiredService<MainForm>();
            await dataRoom.InitializeFromSplashAsync(this, 84);

            ReportInitialization(88, "Connessione alla Rassegna Stampa…");
            await _pressReviewService.VerifyAsync();
            ReportInitialization(96, "Preparazione di Document Hub…");
            DocumentHubForm hub = _services.GetRequiredService<DocumentHubForm>();
            hub.SetPreparedDataRoom(dataRoom);
            ReadyForm = hub;
            ReportInitialization(100, "Document Hub pronto.");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (MsalClientException ex) when (ex.ErrorCode is "authentication_canceled" or "access_denied")
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        catch (Exception ex)
        {
            AppMessageBox.Show(
                ex.Message,
                "Verifica accesso non riuscita",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error,
                this);
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }

    private void ShowDenied(string email, bool configurationMissing)
    {
        using var denied = new ApplicationAccessDeniedForm(email, configurationMissing);
        denied.ShowDialog(this);
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
