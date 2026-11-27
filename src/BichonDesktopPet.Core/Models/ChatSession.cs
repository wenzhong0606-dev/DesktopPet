using System;

namespace BichonDesktopPet.Core.Models;

public sealed class ChatSession
{
    public string Id { get; init; } = string.Empty;
    public string CompanionId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public long MessageCount { get; init; }

    public string DisplayTitle =>
        $"{Title} · {UpdatedAtUtc.ToLocalTime():MM-dd HH:mm}";
}

public sealed class ChatHistoryMessage
{
    public long Id { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
}

public sealed class ChatHistoryPage
{
    public IReadOnlyList<ChatHistoryMessage> Messages { get; init; } = Array.Empty<ChatHistoryMessage>();
    public bool HasMore { get; init; }
    public long? NextBeforeMessageId { get; init; }
}
