namespace BichonDesktopPet.Core.Models;

/// <summary>
/// Lightweight particle state used by the desktop snow animation.
/// Coordinates are expressed in WPF device-independent pixels.
/// </summary>
public sealed class SnowParticle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Radius { get; set; }
    public double FallSpeed { get; set; }
    public double Drift { get; set; }
    public double Opacity { get; set; }
}
