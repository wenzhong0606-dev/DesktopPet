using System;

namespace BichonDesktopPet.Core.Models;

public sealed record CompanionMemory(string Topic, string Content, DateTimeOffset UpdatedAtUtc,
    string Category = "未分类", string Source = "manual", long? SourceMessageId = null,
    string Confidence = "user_confirmed")
{
    public string SourceLabel => Source == "automatic"
        ? $"{Category} · 自动记录 · 用户直接陈述 · 消息 #{SourceMessageId}"
        : $"{Category} · 手动确认";
}
