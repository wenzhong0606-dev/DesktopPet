using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public string SpeciesDisplayName => Species switch
    {
        "dog" => "狗狗",
        "cat" => "猫咪",
        "rabbit" => "兔子",
        "reptile" => "爬行动物",
        "humanoid" => "人形角色",
        _ => "自定义"
    };

    public string ImagePath { get; set; } = string.Empty;

    public bool IsBuiltIn { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
