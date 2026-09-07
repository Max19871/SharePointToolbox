using System;
using System.Collections.Generic;
using System.Text;

namespace SharePointToolbox.Helpers;

/// <summary>
/// Gestisce il log visualizzato nella finestra principale.
/// </summary>
public static class UiLogger
{
    public sealed record Entry(DateTime Timestamp, string Message);

    private const int MaximumEntries = 1000;
    private static readonly object EntriesLock = new();
    private static readonly Queue<Entry> Entries = new();
    private static Action<string>? _writer;
    private static Func<string, Task>? _onlineWriter;
    private static readonly SemaphoreSlim OnlineLock = new(1, 1);

    /// <summary>
    /// Collega il logger alla RichTextBox.
    /// </summary>
    public static void Initialize(Action<string> writer)
    {
        _writer = writer;
    }

    public static void EnableOnlineLogging(Func<string, Task> writer) => _onlineWriter = writer;

    public static void DisableOnlineLogging() => _onlineWriter = null;

    /// <summary>
    /// Aggiunge una riga al log.
    /// </summary>
    public static void Info(string message)
    {
        var entry = new Entry(DateTime.Now, message);
        lock (EntriesLock)
        {
            Entries.Enqueue(entry);
            while (Entries.Count > MaximumEntries) Entries.Dequeue();
        }

        Serilog.Log.Information("{LocalLogMessage}", message);
        _writer?.Invoke($"{entry.Timestamp:HH:mm:ss}  {message}");
        EntryAdded?.Invoke(entry);
        if (_onlineWriter is not null) _ = WriteOnlineAsync(message);
    }

    public static event Action<Entry>? EntryAdded;

    public static IReadOnlyList<Entry> GetSnapshot()
    {
        lock (EntriesLock) return Entries.ToArray();
    }

    private static async Task WriteOnlineAsync(string message)
    {
        await OnlineLock.WaitAsync();
        try
        {
            if (_onlineWriter is not null) await _onlineWriter(message);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Salvataggio del log online non riuscito");
            _writer?.Invoke($"{DateTime.Now:HH:mm:ss}  [ERRORE] Registrazione online del log non riuscita.");
        }
        finally
        {
            OnlineLock.Release();
        }
    }
}
