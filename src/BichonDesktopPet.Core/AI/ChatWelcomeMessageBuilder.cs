using System;
using System.Linq;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Creates a local, persona-aware greeting for an empty chat session.
/// The first configured nickname is the pet's actual name; AvatarProfile.Name is only a management label.
/// The greeting is presentation-only and is never persisted or sent to the AI provider.
/// </summary>
public static class ChatWelcomeMessageBuilder
{
    public static string? GetPrimaryNickname(AvatarProfile? avatar)
    {
        var persona = PersonaProfileValidator.NormalizeOrDefault(avatar?.Persona);
        return persona.Nicknames.FirstOrDefault();
    }

    public static string Build(CompanionIdentity? companion, AvatarProfile? avatar)
    {
        _ = companion; // Stable companion identity is not a conversational display name.
        var persona = PersonaProfileValidator.NormalizeOrDefault(avatar?.Persona);
        var name = GetPrimaryNickname(avatar);
        var userAddress = Clean(persona.UserAddress, 20, "你");
        var selfReference = Clean(persona.SelfReference, 20, "我");
        var directAddress = userAddress is "你" or "您";
        var addressPrefix = directAddress ? string.Empty : userAddress + "，";
        var nameIntroduction = string.IsNullOrWhiteSpace(name) ? string.Empty : $"我是{name}。";

        var greeting = persona.SpeechStyle switch
        {
            "gentle" => $"你好，{addressPrefix}{nameIntroduction}{selfReference}会认真听你说。今天想从哪里聊起？",
            "cool" => $"{addressPrefix}{nameIntroduction}{selfReference}在。想聊什么，直接说吧。",
            "tsundere" => directAddress
                ? $"哼，你终于来啦？{nameIntroduction}{selfReference}才没有特意等你……说吧，今天想聊什么？"
                : $"哼，{userAddress}终于来啦？{nameIntroduction}{selfReference}才没有特意等你……说吧，今天想聊什么？",
            "playful" => $"{addressPrefix}{nameIntroduction}{selfReference}已经准备好了～今天想聊点什么？",
            "natural" => $"你好，{addressPrefix}{nameIntroduction}{selfReference}在这里。今天想聊些什么？",
            _ => $"{addressPrefix}{nameIntroduction}{selfReference}在这里陪你。今天想聊些什么？"
        };

        var traitLine = BuildTraitLine(persona, selfReference);
        if (traitLine.Length > 0)
        {
            greeting += " " + traitLine;
        }
        else if (persona.ResponseLength == "medium")
        {
            greeting += " 不用着急，慢慢说。";
        }

        if (persona.EmojiLevel == "high")
        {
            greeting += " " + GetEmoji(persona.SpeechStyle);
        }

        return greeting;
    }

    private static string BuildTraitLine(PersonaProfile persona, string selfReference)
    {
        foreach (var trait in persona.PersonalityTraits)
        {
            var line = trait.ToLowerInvariant() switch
            {
                "黏人" or "亲人" or "爱撒娇" or "affectionate" or "clingy" => $"其实{selfReference}很喜欢和你待在一起。",
                "好奇" or "curious" => "今天有什么新鲜事，也想听你分享。",
                "活泼" or "调皮" or "playful" or "lively" => "要是有趣的事，记得告诉我！",
                "害羞" or "胆小" or "shy" or "timid" => "刚开始打招呼，还有一点点不好意思。",
                "独立" or "高冷" or "independent" or "aloof" => "你想聊的时候再叫我就好。",
                _ => string.Empty
            };

            if (line.Length > 0)
            {
                return line;
            }
        }

        return string.Empty;
    }

    private static string GetEmoji(string speechStyle) => speechStyle switch
    {
        "cute" => "🐾",
        "gentle" => "🌷",
        "tsundere" => "😼",
        "playful" => "✨",
        _ => "·"
    };

    private static string Clean(string? value, int maxLength, string fallback = "")
    {
        var normalized = (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (normalized.Length > maxLength) normalized = normalized[..maxLength];
        return normalized.Length == 0 ? fallback : normalized;
    }
}
