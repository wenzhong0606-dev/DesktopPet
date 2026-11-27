namespace BichonDesktopPet.Core.Models;

/// <summary>
/// A small, local-only record of an interaction. Raw user conversation text is
/// deliberately not captured by this foundation layer.
/// </summary>
public sealed class CompanionInteractionEvent
{
    public string Id { get; init; } = string.Empty;

    public string CompanionId { get; init; } = string.Empty;

    public string EventType { get; init; } = string.Empty;

    public string PayloadJson { get; init; } = "{}";

    public DateTimeOffset OccurredAtUtc { get; init; }
}
