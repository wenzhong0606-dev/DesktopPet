namespace BichonDesktopPet.Core.Models;

/// <summary>
/// Metadata for one locally available image-based desktop companion.
/// ImagePath is relative to the packaged Assets folder for built-in avatars,
/// or relative to the user's avatars folder for imported avatars.
/// </summary>
public sealed class AvatarProfile
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Simple species/type metadata, such as dog, cat, or custom.</summary>
    public string Species { get; set; } = "custom";

    public string ImagePath { get; set; } = string.Empty;

    public bool IsBuiltIn { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
