namespace BichonDesktopPet.Core.Models;

/// <summary>
/// Maps a high-level behavior state to a user-facing message and a host-renderable effect.
/// Keeping this separate from the state engine allows future avatar-specific presenters.
/// </summary>
public sealed record PetBehaviorPresentation(
    PetBehaviorState State,
    PetVisualEffect VisualEffect,
    string Message);
