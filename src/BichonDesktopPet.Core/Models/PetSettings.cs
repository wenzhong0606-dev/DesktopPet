namespace BichonDesktopPet.Core.Models;

/// <summary>
/// User preferences persisted on the local machine.
/// Window coordinates are nullable so the application can choose a sensible first-run position.
/// </summary>
public sealed class PetSettings
{
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public bool SnowEnabled { get; set; } = true;
    public bool DecorativeLightsEnabled { get; set; } = true;

    /// <summary>Music is deliberately disabled by default.</summary>
    public bool MusicEnabled { get; set; }

    public double WindowWidth { get; set; } = 280;
    public double WindowHeight { get; set; } = 360;
}
