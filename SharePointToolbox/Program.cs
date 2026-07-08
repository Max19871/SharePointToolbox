using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using Microsoft.Kiota.Http.HttpClientLibrary;
using Microsoft.Kiota.Authentication.Azure;
using SharePointToolbox.Configuration;
using SharePointToolbox.Graph;
using SharePointToolbox.UI;
using SharePointToolbox.Microsoft365;

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

        // Configura il logging su file
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Services.AddSerilog();

        // Registra i servizi
        builder.Services.AddSingleton<AuthenticationService>();

        // Registra il servizio SharePoint
        builder.Services.AddSingleton<SharePointService>();

        // Registra il provider che fornisce il token a Microsoft Graph
        builder.Services.AddSingleton<GraphAccessTokenProvider>();

        // Registra il form principale
        builder.Services.AddTransient<MainForm>();
        builder.Services.AddTransient<MainMaterialForm>();

        // Costruisce il contenitore Dependency Injection
        var host = builder.Build();

        ApplicationConfiguration.Initialize();

        // Ottiene il MainForm dal contenitore
        var mainForm = host.Services.GetRequiredService<MainMaterialForm>();

        // Avvia l'applicazione
        Application.Run(mainForm);
    }
}