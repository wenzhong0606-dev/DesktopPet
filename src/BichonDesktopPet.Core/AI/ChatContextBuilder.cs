using System;
using System.Collections.Generic;
using System.Linq;
using BichonDesktopPet.Core.Models;

namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Builds a bounded, provider-neutral prompt from the active companion profile
/// and the in-memory conversation. It deliberately does not retrieve or create long-term memories.
/// </summary>
public static class ChatContextBuilder
{
    public const int DefaultHistoryLimit = 12;
    public const int MaxMessageCharacters = 2000;

    public static IReadOnlyList<AiChatMessage> BuildMessages(
        CompanionIdentity companion,
        AvatarProfile avatar,
        PetBehaviorState currentState,
        IReadOnlyList<AiChatMessage> conversationHistory,
        int historyLimit = DefaultHistoryLimit)
    {
        ArgumentNullException.ThrowIfNull(companion);
        ArgumentNullException.ThrowIfNull(avatar);
        ArgumentNullException.ThrowIfNull(conversationHistory);

        if (historyLimit is < 0 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(historyLimit), "History limit must be between 0 and 50.");
        }

        var persona = avatar.Persona ?? new PersonaProfile();
        var name = Clean(avatar.Name, 60);
        if (name.Length == 0)
        {
            name = Clean(companion.DisplayName, 60);
        }

        if (name.Length == 0)
        {
            name = "桌面伙伴";
        }

        var traits = string.Join("、", persona.PersonalityTraits
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Clean(value, 40))
            .Where(value => value.Length > 0)
            .Take(3));
        var catchphrases = string.Join("；", persona.Catchphrases
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Clean(value, 60))
            .Where(value => value.Length > 0)
            .Take(5));

        var systemPrompt = $"""
            你是{name}，一个长期存在的桌面伙伴。请用自然、真诚、符合设定的方式与用户交流。
            身份与外观：当前角色是{name}（{Clean(avatar.SpeciesDisplayName, 30)}{(string.IsNullOrWhiteSpace(avatar.Breed) ? string.Empty : "，" + Clean(avatar.Breed, 50))}）。
            自称：{Clean(persona.SelfReference, 20, "我")}；称呼用户：{Clean(persona.UserAddress, 20, "你")}。
            语言风格：{Clean(persona.SpeechStyle, 30, "natural")}；回复长度：{Clean(persona.ResponseLength, 20, "short")}；表情符号频率：{Clean(persona.EmojiLevel, 20, "medium")}。
            性格特征：{(traits.Length == 0 ? "温和、友善" : traits)}。
            口头表达参考：{(catchphrases.Length == 0 ? "无" : catchphrases)}。
            当前行为状态：{currentState}。
            保持角色设定，但不要机械地重复设定内容。优先直接回应用户的问题。
            只依据当前对话中实际提供的信息回答；不要假装记得未出现在当前对话里的往事。
            你没有执行系统命令、访问文件或控制操作系统的能力；不要声称已经执行这些操作。
            """;

        var messages = new List<AiChatMessage>
        {
            new("system", systemPrompt)
        };

        if (historyLimit == 0)
        {
            return messages;
        }

        var recent = conversationHistory
            .Where(message =>
                message is not null &&
                (message.Role == "user" || message.Role == "assistant") &&
                !string.IsNullOrWhiteSpace(message.Content))
            .TakeLast(historyLimit);

        foreach (var message in recent)
        {
            messages.Add(new AiChatMessage(message.Role, Clean(message.Content, MaxMessageCharacters)));
        }

        return messages;
    }

    private static string Clean(string? value, int maxLength, string fallback = "")
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength];
        }

        return normalized.Length == 0 ? fallback : normalized;
    }
}
