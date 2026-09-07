using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharePointToolbox.Configuration;
using SharePointToolbox.UI;
using SharePointToolbox.Microsoft365;
using SharePointToolbox.UI.Theming;

using Serilog;

namespace SharePointToolbox;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Crea l'host dell'applicazione (.NET)
        var builder = Host.CreateApplicationBuilder();

        // Carica il file di configurazione
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        // Associa la sezione AzureAd alla classe AzureAdOptions
        builder.Services.Configure<AzureAdOptions>(
            builder.Configuration.GetSection("AzureAd"));

        // Associa la sezione "SharePoint" alla classe SharePointOptions
        builder.Services.Configure<SharePointOptions>(
            builder.Configuration.GetSection("SharePoint"));
        builder.Services.Configure<PressReviewOptions>(
            builder.Configuration.GetSection("PressReview"));

        builder.Services.Configure<BrandingOptions>(
            builder.Configuration.GetSection("Branding"));

        builder.Services.Configure<SecurityOptions>(
            builder.Configuration.GetSection("Security"));

        builder.Services.Configure<MailOptions>(
            builder.Configuration.GetSection("Mail"));

        builder.Services.Configure<ThemeOptions>(
            builder.Configuration.GetSection("Theme"));

        // Configura il logging su file
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Services.AddSerilog();

        // Registra i servizi
        builder.Services.AddSingleton<AuthenticationService>();
        builder.Services.AddSingleton<ApplicationAuthorizationSession>();

        // Registra il servizio SharePoint
        builder.Services.AddSingleton<SharePointService>();
        builder.Services.AddSingleton<PressReviewService>();
        builder.Services.AddSingleton<AppThemeManager>();

        // Registra il form principale
        builder.Services.AddTransient<MainForm>();
        builder.Services.AddTransient<DocumentHubForm>();
        builder.Services.AddTransient<PressReviewForm>();
        builder.Services.AddTransient<StartupAuthorizationForm>();

        // Costruisce il contenitore Dependency Injection
        using var host = builder.Build();

        ApplicationConfiguration.Initialize();
        Application.SetDefaultFont(AppTypography.Create(AppTypography.BodySize));

        try
        {
            using StartupAuthorizationForm authorizationGate =
                host.Services.GetRequiredService<StartupAuthorizationForm>();
            if (authorizationGate.ShowDialog() != DialogResult.OK)
                return;

            if (authorizationGate.ReadyForm is null)
                return;
            Application.Run(authorizationGate.ReadyForm);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
