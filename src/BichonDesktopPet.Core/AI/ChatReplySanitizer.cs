using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BichonDesktopPet.Core.AI;

/// <summary>
/// Removes clearly marked role-play stage directions from provider output while
/// preserving ordinary parenthetical explanations and the actual answer text.
/// </summary>
public static class ChatReplySanitizer
{
    private static readonly Regex ParentheticalText = new(
        @"[（(]([^（）()]{1,48})[）)]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex AsteriskText = new(
        @"\*([^*]{1,80})\*",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] StageDirectionIndicators =
    [
        "摇尾巴", "眨眼", "眨眨眼", "微笑", "露出笑容", "笑了笑", "点点头", "点头",
        "歪头", "蹭蹭", "伸懒腰", "打哈欠", "跳起来", "扑过来", "挥挥手",
        "表情", "神情", "动作", "开心地", "兴奋地", "看着你", "望向你", "尾巴"
    ];

    public static string Sanitize(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var sanitized = ParentheticalText.Replace(content, match =>
            IsStageDirection(match.Groups[1].Value) ? string.Empty : match.Value);
        sanitized = AsteriskText.Replace(sanitized, match =>
            IsStageDirection(match.Groups[1].Value) ? string.Empty : match.Value);

        sanitized = Regex.Replace(sanitized, @"[ \t]{2,}", " ");
        sanitized = Regex.Replace(sanitized, @"[ \t]+([，。！？；：,.!?;:])", "$1");
        return sanitized.Trim();
    }

    private static bool IsStageDirection(string content) =>
        StageDirectionIndicators.Any(indicator =>
            content.Contains(indicator, StringComparison.OrdinalIgnoreCase));
}
