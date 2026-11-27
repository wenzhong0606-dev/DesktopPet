namespace BichonDesktopPet.Core.Models;

/// <summary>
/// A persistent digital companion identity, kept separate from the avatar
/// currently used to present it. Replacing an avatar must not replace this ID.
/// </summary>
public sealed class CompanionIdentity
{
    public string Id { get; init; } = string.Empty;

    public string AvatarId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    /// <summary>A snapshot of the companion's initial, user-configured persona.</summary>
    public string CorePersonalityJson { get; init; } = "{}";

    /// <summary>Reserved for gradual, evidence-based personality development.</summary>
    public string EvolvingPersonalityJson { get; init; } = "{}";

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset UpdatedAtUtc { get; init; }

    public bool IsArchived { get; init; }
}
