using System;
using System.Collections.Generic;
using System.Text;

namespace SharePointToolbox.Helpers;

/// <summary>
/// Gestisce il log visualizzato nella finestra principale.
/// </summary>
public static class UiLogger
{
    private static Action<string>? _writer;

    /// <summary>
    /// Collega il logger alla RichTextBox.
    /// </summary>
    public static void Initialize(Action<string> writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Aggiunge una riga al log.
    /// </summary>
    public static void Info(string message)
    {
        _writer?.Invoke($"{DateTime.Now:HH:mm:ss}  {message}");
    }
}