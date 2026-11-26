using System.Text.Json;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.App.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BichonDesktopPet",
        "settings.json");

    public PetSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new PetSettings();
            }

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<PetSettings>(json) ?? new PetSettings();
        }
        catch
        {
            return new PetSettings();
        }
    }

    public void Save(PetSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // Settings persistence is best-effort; the desktop pet remains usable.
        }
    }
}
