using System;
using System.Collections.Generic;
using System.Linq;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.Services;

/// <summary>
/// Shared validation for persona data entered in the UI and loaded from local JSON.
/// </summary>
public static class PersonaProfileValidator
{
    private static readonly HashSet<string> SpeechStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "cute", "gentle", "cool", "tsundere", "playful", "natural"
    };

    private static readonly HashSet<string> ResponseLengths = new(StringComparer.OrdinalIgnoreCase)
    {
        "short", "medium"
    };

    private static readonly HashSet<string> EmojiLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "low", "medium", "high"
    };

    public static PersonaProfile CreateDefault() => new();

    public static bool TryNormalize(
        PersonaProfile? source,
        out PersonaProfile normalized,
        out string error)
    {
        normalized = CreateDefault();
        error = string.Empty;

        if (source is null)
        {
            return true;
        }

        var selfReference = source.SelfReference?.Trim() ?? string.Empty;
        if (selfReference.Length is < 1 or > 20)
        {
            error = "宠物自称必须为 1 到 20 个字符。";
            return false;
        }

        var userAddress = source.UserAddress?.Trim() ?? string.Empty;
        if (userAddress.Length is < 1 or > 20)
        {
            error = "用户称呼必须为 1 到 20 个字符。";
            return false;
        }

        if (!TryNormalizeList(source.Nicknames, 5, 20, "昵称", out var nicknames, out error) ||
            !TryNormalizeList(source.PersonalityTraits, 3, 16, "性格特征", out var traits, out error) ||
            !TryNormalizeList(source.Catchphrases, 5, 40, "口头禅", out var catchphrases, out error))
        {
            return false;
        }

        var speechStyle = NormalizeChoice(source.SpeechStyle, SpeechStyles, "cute");
        var responseLength = NormalizeChoice(source.ResponseLength, ResponseLengths, "short");
        var emojiLevel = NormalizeChoice(source.EmojiLevel, EmojiLevels, "medium");

        normalized = new PersonaProfile
        {
            Nicknames = nicknames,
            SelfReference = selfReference,
            UserAddress = userAddress,
            PersonalityTraits = traits,
            SpeechStyle = speechStyle,
            ResponseLength = responseLength,
            EmojiLevel = emojiLevel,
            Catchphrases = catchphrases
        };

        return true;
    }

    public static PersonaProfile NormalizeOrDefault(PersonaProfile? source)
    {
        return TryNormalize(source, out var normalized, out _)
            ? normalized
            : CreateDefault();
    }

    private static bool TryNormalizeList(
        IEnumerable<string>? source,
        int maximumCount,
        int maximumLength,
        string label,
        out List<string> normalized,
        out string error)
    {
        normalized = [];
        error = string.Empty;

        foreach (var rawValue in source ?? [])
        {
            var value = rawValue?.Trim() ?? string.Empty;
            if (value.Length == 0)
            {
                continue;
            }

            if (value.Length > maximumLength)
            {
                error = $"{label}单项不能超过 {maximumLength} 个字符。";
                return false;
            }

            if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
            {
                normalized.Add(value);
            }
        }

        if (normalized.Count > maximumCount)
        {
            error = $"{label}最多填写 {maximumCount} 项。";
            return false;
        }

        return true;
    }

    private static string NormalizeChoice(
        string? value,
        HashSet<string> allowedValues,
        string fallback)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return allowedValues.Contains(normalized) ? normalized.ToLowerInvariant() : fallback;
    }
}
