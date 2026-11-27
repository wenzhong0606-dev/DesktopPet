using System;
using System.IO;

namespace BichonDesktopPet.App.Services;

/// <summary>
/// Writes diagnostic entries to the user's local application-data directory.
/// The logger deliberately records application diagnostics only, not conversations or input text.
/// </summary>
public static class AppLogger
{
    private static readonly object SyncRoot = new();

    private static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BichonDesktopPet",
        "logs",
        "app.log");

    public static void Log(string message, Exception? exception = null)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var entry = $"{DateTimeOffset.Now:O} [ERROR] {message}";
            if (exception is not null)
            {
                entry += Environment.NewLine + exception;
            }

            lock (SyncRoot)
            {
                File.AppendAllText(LogPath, entry + Environment.NewLine + Environment.NewLine);
            }
        }
        catch
        {
            // Logging is best-effort and must not cause a second application failure.
        }
    }
}
