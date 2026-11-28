using System;

namespace BichonDesktopPet.Core.Models;

public sealed record CompanionMemory(string Topic, string Content, DateTimeOffset UpdatedAtUtc,
    string Category = "未分类", string Source = "manual", long? SourceMessageId = null,
    string Confidence = "user_confirmed", bool Important = false, bool Archived = false,
    DateTimeOffset? ExpiresAtUtc = null, long? SourceReplyId = null)
{
    public bool IsAvailableAt(DateTimeOffset now) => !Archived &&
        (ExpiresAtUtc is null || ExpiresAtUtc.Value > now);

    public string SourceLabel => (Source == "automatic"
        ? $"{Category} · 自动记录 · {(Confidence == "observed_exchange" ? "本机对话记录" : Confidence == "cloud_selected_literal" ? "Qwen辅助选择 · 本机校验原文" : Confidence == "open_explicit_statement" ? "开放词汇 · 用户直接陈述" : "用户直接陈述")} · 消息 #{SourceMessageId}"
        : $"{Category} · 手动确认") +
        (Source == "automatic" && SourceReplyId is { } reply ? $" / 回复 #{reply}" : "") +
        (Important ? " · 重要" : "") +
        (Archived ? " · 已归档" : ExpiresAtUtc is { } expiry
            ? (expiry <= DateTimeOffset.UtcNow ? " · 已过期" : $" · 有效至 {expiry.ToLocalTime():yyyy-MM-dd}") : "");
}
