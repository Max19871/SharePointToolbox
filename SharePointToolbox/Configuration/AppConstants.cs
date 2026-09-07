namespace SharePointToolbox.Configuration;

/// <summary>
/// Raccolta di costanti globali per l'applicazione.
/// </summary>
public static class AppConstants
{
    // Cambiando questa stringa qui, cambierà in tutto il software (Form, Log, MessageBox)
    public const string AppName = "Document Hub";
    public const int GraphRequestTimeoutSeconds = 60;
    public const int ExpirationWarningDays = 15;
    public const string DisclaimerVersion = "2026.01";
    public const string DisclaimerText =
        "Questa applicazione è destinata esclusivamente a personale autorizzato. " +
        "L’accesso ai documenti e alle informazioni presenti nel portale SharePoint deve avvenire " +
        "per finalità professionali legittime, secondo il principio del minimo privilegio e nel " +
        "rispetto delle procedure di sicurezza e riservatezza dello Studio.\n\n" +
        "In applicazione del sistema di gestione certificato dello Studio e dei requisiti delle " +
        "norme ISO 9001, ISO/IEC 27001 e UNI 11871, l’utente deve verificare destinatari, " +
        "autorizzazioni e scadenze prima di creare o modificare una condivisione; non deve condividere " +
        "credenziali o dati con soggetti non autorizzati; deve inoltre segnalare tempestivamente accessi " +
        "anomali, errori o possibili incidenti di sicurezza. Le attività possono essere registrate nei " +
        "sistemi Microsoft 365 e sottoposte a controllo secondo le policy applicabili.";
}
