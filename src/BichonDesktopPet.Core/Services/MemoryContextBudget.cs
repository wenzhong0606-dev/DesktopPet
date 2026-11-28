using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>Limits actual serialized memory payload bytes, not a provider tokenizer count.</summary>
public static class MemoryContextBudget
{
    public const int MaximumPayloadBytes = 2400;
    public const int MaximumEntries = 12;
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private sealed record Entry(string topic, string content, string source);

    public static IReadOnlyList<CompanionMemory> Select(IEnumerable<CompanionMemory> ranked,
        int byteBudget = MaximumPayloadBytes, DateTimeOffset? now = null)
    {
        if (byteBudget < 2 || byteBudget > MaximumPayloadBytes)
            throw new ArgumentOutOfRangeException(nameof(byteBudget));
        var selected = new List<CompanionMemory>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var bytes = 2; // brackets
        var timestamp = now ?? DateTimeOffset.UtcNow;
        foreach (var memory in ranked)
        {
            if (selected.Count >= MaximumEntries) break;
            if (!memory.IsAvailableAt(timestamp) || string.IsNullOrWhiteSpace(memory.Topic)) continue;
            var key = MemoryVocabulary.CanonicalTopic(memory.Topic);
            if (keys.Contains(key)) continue;
            var cost = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(ToEntry(memory), Options))
                + (selected.Count == 0 ? 0 : 1);
            if (bytes + cost > byteBudget) continue;
            selected.Add(memory);
            keys.Add(key);
            bytes += cost;
        }
        return selected;
    }

    public static string BuildJson(IEnumerable<CompanionMemory> ranked) =>
        JsonSerializer.Serialize(System.Linq.Enumerable.Select(Select(ranked), ToEntry), Options);

    private static Entry ToEntry(CompanionMemory memory) => new(
        Clean(memory.Topic, CompanionMemoryStore.MaximumTopicCharacters),
        Clean(memory.Content, CompanionMemoryStore.MaximumContentCharacters),
        memory.Source != "automatic" ? "用户手动确认" :
            memory.Confidence == "observed_exchange" ? "本机对话记录（只证实已回复）" :
            memory.Confidence == "cloud_selected_literal" ? "用户直接陈述（Qwen辅助选择、本机校验）" : "用户直接陈述（自动记录）");

    private static string Clean(string? value, int max)
    {
        var text = value?.Trim() ?? "";
        return text.Length <= max ? text : text[..max];
    }
}
