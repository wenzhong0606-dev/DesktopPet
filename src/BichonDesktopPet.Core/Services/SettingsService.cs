using System.IO;
using System.Text.Json;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Loads and saves local desktop-pet settings. The file path and diagnostic callback
/// are injectable so persistence behavior can be tested without touching user data.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly Action<string, Exception?>? _log;

    public SettingsService(
        string? settingsPath = null,
        Action<string, Exception?>? log = null)
    {
        _settingsPath = string.IsNullOrWhiteSpace(settingsPath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BichonDesktopPet",
                "settings.json")
            : settingsPath;

        _log = log;
    }

    public string SettingsPath => _settingsPath;

    public PetSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new PetSettings();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<PetSettings>(json, JsonOptions) ?? new PetSettings();
        }
        catch (Exception exception)
        {
            Report("Unable to read desktop-pet settings. Default settings will be used.", exception);
            return new PetSettings();
        }
    }

    public void Save(PetSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? temporaryPath = null;

        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("The settings file must have a parent directory.");
            }

            Directory.CreateDirectory(directory);
            temporaryPath = _settingsPath + ".tmp";

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsPath, overwrite: true);
        }
        catch (Exception exception)
        {
            Report("Unable to save desktop-pet settings. The application will continue with in-memory settings.", exception);

            if (!string.IsNullOrWhiteSpace(temporaryPath))
            {
                try
                {
                    if (File.Exists(temporaryPath))
                    {
                        File.Delete(temporaryPath);
                    }
                }
                catch
                {
                    // Cleanup is best-effort; the original persistence error is already logged.
                }
            }
        }
    }

    private void Report(string message, Exception exception)
    {
        try
        {
            _log?.Invoke(message, exception);
        }
        catch
        {
            // Diagnostics must never prevent the desktop pet from starting or running.
        }
    }
}
