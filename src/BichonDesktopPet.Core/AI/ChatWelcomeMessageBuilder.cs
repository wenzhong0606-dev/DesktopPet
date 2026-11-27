using System;
using System.Linq;
using BichonDesktopPet.Core.Models;
using BichonDesktopPet.Core.Services;

namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Creates a local, persona-aware greeting for an empty chat session.
/// The greeting is presentation-only and must never be added to persisted history or AI context.
/// </summary>
public static class ChatWelcomeMessageBuilder
{
    public static string Build(CompanionIdentity? companion, AvatarProfile? avatar)
    {
        var persona = PersonaProfileValidator.NormalizeOrDefault(avatar?.Persona);
        var name = Clean(avatar?.Name, 40);
        if (name.Length == 0)
        {
            name = Clean(companion?.DisplayName, 40);
        }

        if (name.Length == 0)
        {
            name = "桌面伙伴";
        }

        var userAddress = Clean(persona.UserAddress, 20, "你");
        var selfReference = Clean(persona.SelfReference, 20, "我");
        var directAddress = userAddress is "你" or "您";

        var greeting = persona.SpeechStyle switch
        {
            "gentle" => directAddress
                ? $"你好，我是{name}。{selfReference}会在这里认真听你说，今天想从哪里聊起？"
                : $"你好，{userAddress}。我是{name}。{selfReference}会在这里认真听你说，今天想从哪里聊起？",
            "cool" => directAddress
                ? $"我是{name}。{selfReference}在。想聊什么，直接说吧。"
                : $"我是{name}。{selfReference}在，{userAddress}。想聊什么，直接说吧。",
            "tsundere" => directAddress
                ? $"哼，你终于来啦？我是{name}。{selfReference}才没有特意等你……说吧，今天想聊什么？"
                : $"哼，{userAddress}终于来啦？我是{name}。{selfReference}才没有特意等你……说吧，今天想聊什么？",
            "playful" => directAddress
                ? $"你来啦！我是{name}，{selfReference}已经就位～今天想聊点什么，还是先来玩一会儿？"
                : $"{userAddress}来啦！我是{name}，{selfReference}已经就位～今天想聊点什么，还是先来玩一会儿？",
            "natural" => directAddress
                ? $"你好，我是{name}。今天想聊些什么？"
                : $"你好，{userAddress}。我是{name}，今天想聊些什么？",
            _ => directAddress
                ? $"嗨！我是{name}，{selfReference}已经准备好陪你聊天啦～今天想聊些什么？"
                : $"嗨，{userAddress}！我是{name}，{selfReference}已经准备好陪你聊天啦～今天想聊些什么？"
        };

        var traitLine = BuildTraitLine(persona, selfReference);
        if (traitLine.Length > 0)
        {
            greeting += " " + traitLine;
        }
        else if (persona.ResponseLength == "medium")
        {
            greeting += $" 慢慢来，{selfReference}会认真听你说。";
        }

        if (persona.ResponseLength == "medium")
        {
            var nickname = persona.Nicknames.FirstOrDefault(value =>
                !string.Equals(value, name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(nickname))
            {
                greeting += $" 对了，你也可以叫{selfReference}“{Clean(nickname, 20)}”。";
            }

            var catchphrase = persona.Catchphrases.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(catchphrase))
            {
                greeting += " " + Clean(catchphrase, 40);
            }
        }

        var emoji = GetEmoji(persona.SpeechStyle);
        if (persona.EmojiLevel == "high" ||
            (persona.EmojiLevel == "medium" && persona.SpeechStyle is "cute" or "playful"))
        {
            greeting += " " + emoji;
        }

        return greeting;
    }

    private static string BuildTraitLine(PersonaProfile persona, string selfReference)
    {
        foreach (var trait in persona.PersonalityTraits)
        {
            var line = trait.ToLowerInvariant() switch
            {
                "黏人" or "亲人" or "爱撒娇" or "affectionate" or "clingy" =>
                    $"其实{selfReference}很喜欢和你待在一起。",
                "好奇" or "curious" =>
                    "今天有什么新鲜事，也想听你分享。",
                "活泼" or "调皮" or "playful" or "lively" =>
                    $"要是有趣的事，记得告诉{selfReference}！",
                "害羞" or "胆小" or "shy" or "timid" =>
                    "刚开始打招呼，还有一点点不好意思。",
                "独立" or "高冷" or "independent" or "aloof" =>
                    $"你想聊的时候再叫{selfReference}就好。",
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
        _ => "💬"
    };

    private static string Clean(string? value, int maxLength, string fallback = "")
    {
        var normalized = (value ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return normalized.Length == 0 ? fallback : normalized;
    }
}
